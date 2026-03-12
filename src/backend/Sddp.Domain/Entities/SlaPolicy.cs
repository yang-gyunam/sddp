using Sddp.Abstractions.Base;
using Sddp.Abstractions.ValueObjects;

namespace Sddp.Domain.Entities;

/// <summary>
/// SLA entity — project Approval SLA settings
/// </summary>
public class SlaPolicy : EntityBase
{
    public GlobalUniqueId TenantId { get; private set; }
    public GlobalUniqueId ProjectId { get; private set; }

    /// <summary>
    /// SLA type (signoff, review, decision)
    /// </summary>
    public string SlaType { get; private set; } = "signoff";

    /// <summary>
    /// SLA (unit, default 24)
    /// </summary>
    public int SlaHours { get; private set; } = 24;

    /// <summary>
    /// SLA (unit, default 4)
    /// </summary>
    public int UrgentSlaHours { get; private set; } = 4;

    /// <summary>
    /// (%,)
    /// </summary>
    public string ReminderAtPercent { get; private set; } = "50,100,200";

    /// <summary>
    /// role (: PRODUCT_OWNER)
    /// </summary>
    public string? EscalationRole { get; private set; }

    // EF Core default create
    private SlaPolicy() { }

    public SlaPolicy(
        GlobalUniqueId tenantId,
        GlobalUniqueId projectId,
        string slaType,
        int slaHours,
        int urgentSlaHours,
        string? reminderAtPercent = null,
        string? escalationRole = null)
    {
        TenantId = tenantId;
        ProjectId = projectId;
        SlaType = slaType;
        SlaHours = slaHours;
        UrgentSlaHours = urgentSlaHours;
        ReminderAtPercent = reminderAtPercent ?? "50,100,200";
        EscalationRole = escalationRole;
    }

    public void Update(
        int slaHours,
        int urgentSlaHours,
        string? reminderAtPercent,
        string? escalationRole)
    {
        SlaHours = slaHours;
        UrgentSlaHours = urgentSlaHours;
        if (reminderAtPercent is not null)
            ReminderAtPercent = reminderAtPercent;
        EscalationRole = escalationRole;
        MarkAsModified();
    }

    /// <summary>
    /// (: "50,100,200" → [50, 100, 200])
    /// </summary>
    public int[] GetReminderThresholds()
    {
        return ReminderAtPercent
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(s => int.TryParse(s, out var v) ? v : 0)
            .Where(v => v > 0)
            .OrderBy(v => v)
            .ToArray();
    }
}
