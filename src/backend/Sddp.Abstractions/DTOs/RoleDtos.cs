using Sddp.Abstractions.Enums;

namespace Sddp.Abstractions.DTOs;

public record RoleDto(
    string Id,
    string Name,
    string Description,
    RoleType Type,
    bool IsSystemRole,
    IEnumerable<string> Permissions);
