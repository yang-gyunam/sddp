using Sddp.Abstractions.ValueObjects;

namespace Sddp.Abstractions.DTOs;

/// <summary>
/// audit log DTO
/// </summary>
public record AuditLogEntry(
    GlobalUniqueId Id,
    GlobalUniqueId? ActorId,
    string Action,
    string ResourceType,
    GlobalUniqueId ResourceId,
    string? Payload,
    Timestamp CreatedAt);
