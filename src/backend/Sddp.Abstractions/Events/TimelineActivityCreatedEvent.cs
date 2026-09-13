using Sddp.Abstractions.DTOs;

namespace Sddp.Abstractions.Events;

public sealed record TimelineActivityCreatedEvent(
    string TenantId,
    string? ProjectId,
    AuditLogDto Activity);
