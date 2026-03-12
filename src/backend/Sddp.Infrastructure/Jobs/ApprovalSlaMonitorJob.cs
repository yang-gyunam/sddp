using System.Data;
using Dapper;
using Microsoft.Extensions.Logging;

namespace Sddp.Infrastructure.Jobs;

/// <summary>
/// Approval SLA (15)
/// SignOff SLA notification
/// </summary>
public class ApprovalSlaMonitorJob
{
    private readonly IDbConnection _connection;
    private readonly ILogger<ApprovalSlaMonitorJob> _logger;

    public ApprovalSlaMonitorJob(
        IDbConnection connection,
        ILogger<ApprovalSlaMonitorJob> logger)
    {
        _connection = connection;
        _logger = logger;
    }

    public async Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting Approval SLA monitoring scan");

        // 1. pending SignOff + SLA get
        var pendingSignOffs = await (_connection.QueryAsync<PendingSignOffRow>(
            new CommandDefinition(@"
                SELECT
                    so.id AS sign_off_id,
                    so.tenant_id,
                    so.project_id,
                    so.spec_id,
                    so.stakeholder_id,
                    so.requested_at,
                    s.code AS spec_code,
                    p.display_name AS stakeholder_name,
                    COALESCE(sp.sla_hours, 24) AS sla_hours,
                    COALESCE(sp.reminder_at_percent, '50,100,200') AS reminder_at_percent
                FROM sign_offs so
                INNER JOIN specs s ON s.id = so.spec_id AND s.is_active = true
                INNER JOIN users u ON u.id = so.stakeholder_id
                INNER JOIN persons p ON p.id = u.person_id
                LEFT JOIN sla_policies sp
                    ON sp.tenant_id = so.tenant_id
                    AND sp.project_id = so.project_id
                    AND sp.sla_type = 'signoff'
                    AND sp.is_active = true
                WHERE so.is_active = true
                    AND so.signed_at IS NULL",
                cancellationToken: cancellationToken))).ConfigureAwait(false);

        var rows = pendingSignOffs.ToList();

        if (rows.Count == 0)
        {
            _logger.LogDebug("No pending SignOffs found");
            return;
        }

        // 2. notification get ()
        var signOffIds = rows.Select(r => r.SignOffId).ToArray();
        var existingNotifications = await (_connection.QueryAsync<ExistingNotificationRow>(
            new CommandDefinition(@"
                SELECT sign_off_id, threshold_percent
                FROM sla_notifications
                WHERE sign_off_id = ANY(@SignOffIds)
                    AND is_active = true",
                new { SignOffIds = signOffIds },
                cancellationToken: cancellationToken))).ConfigureAwait(false);

        var notifiedSet = existingNotifications
            .Select(n => (n.SignOffId, n.ThresholdPercent))
            .ToHashSet();

        // 3. SignOff SLA + new
        var now = DateTimeOffset.UtcNow;
        var newNotifications = 0;

        foreach (var row in rows)
        {
            var elapsed = (now - row.RequestedAt).TotalHours;
            var percent = (int)(elapsed / row.SlaHours * 100);

            var thresholds = ParseThresholds(row.ReminderAtPercent);

            foreach (var threshold in thresholds)
            {
                if (percent < threshold)
 break; // 

                if (notifiedSet.Contains((row.SignOffId, threshold)))
 continue; // notification 

                var notificationType = threshold switch
                {
                    >= 200 => "warning",
                    >= 100 => "escalation",
                    _ => "reminder"
                };

                // sla_notifications
                await (_connection.ExecuteAsync(
                    new CommandDefinition(@"
                        INSERT INTO sla_notifications
                            (id, tenant_id, project_id, sign_off_id, threshold_percent,
                             notification_type, notified_at, created_at, updated_at, is_active)
                        VALUES
                            (uuid_generate_v4(), @TenantId, @ProjectId, @SignOffId, @Threshold,
                             @Type, NOW(), NOW(), NOW(), true)
                        ON CONFLICT (sign_off_id, threshold_percent) DO NOTHING",
                        new
                        {
                            TenantId = row.TenantId,
                            ProjectId = row.ProjectId,
                            SignOffId = row.SignOffId,
                            Threshold = threshold,
                            Type = notificationType
                        },
                        cancellationToken: cancellationToken))).ConfigureAwait(false);

                newNotifications++;

                _logger.LogInformation(
                    "SLA {NotificationType} for SignOff {SignOffId} (Spec {SpecCode}, Stakeholder {StakeholderName}): " +
                    "{ElapsedHours:F1}h / {SlaHours}h ({Percent}%)",
                    notificationType, row.SignOffId, row.SpecCode, row.StakeholderName,
                    elapsed, row.SlaHours, percent);
            }
        }

        _logger.LogInformation(
            "Approval SLA scan complete: {PendingCount} pending, {NewNotifications} new notifications",
            rows.Count, newNotifications);
    }

    private static int[] ParseThresholds(string reminderAtPercent)
    {
        return reminderAtPercent
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(s => int.TryParse(s, out var v) ? v : 0)
            .Where(v => v > 0)
            .OrderBy(v => v)
            .ToArray();
    }

    #region Internal Row Models

    private record PendingSignOffRow(
        Guid SignOffId,
        Guid TenantId,
        Guid ProjectId,
        Guid SpecId,
        Guid StakeholderId,
        DateTimeOffset RequestedAt,
        string SpecCode,
        string StakeholderName,
        int SlaHours,
        string ReminderAtPercent);

    private record ExistingNotificationRow(
        Guid SignOffId,
        int ThresholdPercent);

    #endregion
}
