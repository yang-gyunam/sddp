using Sddp.Abstractions.Enums;
using Sddp.Abstractions.ValueObjects;

namespace Sddp.Domain.Events;

/// <summary>
/// Spec status changed event
/// </summary>
public sealed record SpecStatusChangedEvent(
    GlobalUniqueId SpecId,
    GlobalUniqueId TenantId,
    GlobalUniqueId ProjectId,
    SpecStatus FromStatus,
    SpecStatus ToStatus,
    GlobalUniqueId ActorId,
    string? Reason = null)
    : AggregateEvent(SpecId, "Spec");
