using Sddp.Abstractions.Base;
using Sddp.Abstractions.ValueObjects;

namespace Sddp.Domain.Entities;

/// <summary>
/// SLA notification record entity for duplicate-prevention delivery tracking.
/// </summary>
public class SlaNotification : EntityBase
{
    public GlobalUniqueId TenantId { get; private set; }
    public GlobalUniqueId ProjectId { get; private set; }

    /// <summary>
    /// Target SignOff ID.
    /// </summary>
    public GlobalUniqueId SignOffId { get; private set; }

    /// <summary>
    /// Notification threshold in percent (for example, 50, 100, 200).
    /// </summary>
    public int ThresholdPercent { get; private set; }

    /// <summary>
    /// Notification type (reminder, escalation, warning).
    /// </summary>
    public string NotificationType { get; private set; } = string.Empty;

    /// <summary>
    /// Notification timestamp.
    /// </summary>
    public Timestamp NotifiedAt { get; private set; } = Timestamp.Now;

    /// <summary>
    /// Related SignOff navigation property.
    /// </summary>
    public SignOff SignOff { get; private set; } = null!;

    // Parameterless constructor for EF Core.
    private SlaNotification() { }

    public SlaNotification(
        GlobalUniqueId tenantId,
        GlobalUniqueId projectId,
        GlobalUniqueId signOffId,
        int thresholdPercent,
        string notificationType)
    {
        TenantId = tenantId;
        ProjectId = projectId;
        SignOffId = signOffId;
        ThresholdPercent = thresholdPercent;
        NotificationType = notificationType;
        NotifiedAt = Timestamp.Now;
    }
}
