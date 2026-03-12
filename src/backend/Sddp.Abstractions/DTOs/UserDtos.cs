namespace Sddp.Abstractions.DTOs;

public record UserDto(
    string Id,
    string Username,
    string Email,
    string DisplayName,
    string? AvatarUrl,
    bool IsActive,
    string? LastLoginAt,
    string CreatedAt);

public record SystemUserDto(
    string Id,
    string Name,
    string Email,
    string Username,
    string GlobalRole,
    string Status,
    bool IsBuiltIn,
    IReadOnlyList<UserProjectDto> Projects,
    string CreatedAt,
    string? LastLoginAt);

public record UserProjectDto(
    string ProjectId,
    string ProjectName,
    string Role);

public record UpdateProfileDto(
    string DisplayName,
    string Email,
    string? AvatarUrl = null);

public record CreateUserDto(
    string Username,
    string Email,
    string DisplayName,
    string Password);

public record UpdateSystemUserDto(
    string DisplayName,
    string Email,
    string? GlobalRole);

public record UserPreferencesDto(
    object? Preferences);

public record NotificationSettingsDto(
    NotificationEmailDto Email,
    NotificationBrowserDto Browser,
    NotificationChannelsDto Channels);

public record NotificationEmailDto(
    bool Mentions,
    bool Conversations,
    bool SpecApprovals,
    bool TaskAssignments,
    bool DailyDigest);

public record NotificationBrowserDto(
    bool Enabled,
    bool Sound,
    bool Preview);

public record NotificationChannelsDto(
    string Default,
    Dictionary<string, string>? Custom);
