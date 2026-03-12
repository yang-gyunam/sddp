namespace Sddp.Abstractions.DTOs;

public record PermissionDto(
    string Id,
    string Code,
    string Name,
    string Resource,
    string Action,
    string? Description);
