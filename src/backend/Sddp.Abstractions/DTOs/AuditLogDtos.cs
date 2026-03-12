namespace Sddp.Abstractions.DTOs;

public record AuditLogDto(
    string Id,
    string Timestamp,
    string UserId,
    string UserName,
    string Action,
    string ResourceType,
    string ResourceId,
    string IpAddress,
    Dictionary<string, object>? Details);

public record FieldAuthorDto(
    string FieldName,
    string UserId,
    string UserName,
    string Timestamp);
