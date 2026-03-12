namespace Sddp.Abstractions.DTOs;

/// <summary>
/// SLA DTO
/// </summary>
public record SlaPolicyDto(
    string Id,
    string TenantId,
    string ProjectId,
    string SlaType,
    int SlaHours,
    int UrgentSlaHours,
    string ReminderAtPercent,
    string? EscalationRole,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

/// <summary>
/// SLA create
/// </summary>
public record CreateSlaPolicyRequest(
    string SlaType,
    int SlaHours,
    int UrgentSlaHours,
    string? ReminderAtPercent,
    string? EscalationRole);

/// <summary>
/// SLA update
/// </summary>
public record UpdateSlaPolicyRequest(
    int SlaHours,
    int UrgentSlaHours,
    string? ReminderAtPercent,
    string? EscalationRole);

/// <summary>
/// SignOff SLA status DTO (dashboard)
/// </summary>
public record PendingSignOffSlaDto(
    string SignOffId,
    string SpecId,
    string SpecCode,
    string SpecTitle,
    string StakeholderName,
    DateTimeOffset RequestedAt,
    int SlaHours,
    double ElapsedHours,
    int ElapsedPercent,
    string Status);
