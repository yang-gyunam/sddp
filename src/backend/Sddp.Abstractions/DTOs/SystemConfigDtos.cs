namespace Sddp.Abstractions.DTOs;

/// <summary>
/// all system settings DTO
/// </summary>
public record SystemConfigDto(
    GeneralConfigDto General,
    AuthConfigDto Auth,
    StorageConfigDto Storage,
    PerformanceConfigDto Performance,
    AiAgentConfigDto AiAgent);

/// <summary>
/// General settings DTO
/// </summary>
public record GeneralConfigDto(
    string SiteName,
    string SiteUrl,
    string AdminEmail,
    string DefaultLocale = "ko-KR",
    string DefaultTimezone = "Asia/Seoul");

/// <summary>
/// Auth settings DTO
/// </summary>
public record AuthConfigDto(
    int SessionTimeout,
    bool StrongPassword,
    int PasswordMinLength,
    bool TwoFactorAuth,
    bool SsoEnabled,
    int MaxFailedLogins = 5,
    int LockoutDuration = 15);

/// <summary>
/// Storage settings DTO
/// </summary>
public record StorageConfigDto(
    string Database,
    decimal StorageUsed,
    decimal StorageLimit,
    int MaxUploadSize = 50);

/// <summary>
/// Performance settings DTO
/// </summary>
public record PerformanceConfigDto(
    bool CacheEnabled,
    int CacheTtlSeconds,
    bool CdnEnabled,
    bool CompressionEnabled,
    bool RateLimitEnabled = true,
    int RateLimitPerMinute = 100);

/// <summary>
/// AI Agent settings DTO
/// </summary>
public record AiAgentConfigDto(
    bool Enabled,
    string Provider,
    string Model,
    string Endpoint,
    string? ApiKey,
    int MaxTokens = 4096,
    decimal Temperature = 0.7m);

/// <summary>
/// settings DTO
/// </summary>
public record SystemConfigItemDto(
    string ConfigGroup,
    string ConfigKey,
    string? Value,
    string ValueType,
    string? DisplayName,
    string? Description,
    bool IsSensitive,
    bool IsReadonly);

/// <summary>
/// settings DTO
/// </summary>
public record UpdateSystemConfigDto(
    UpdateGeneralConfigDto? General = null,
    UpdateAuthConfigDto? Auth = null,
    UpdateStorageConfigDto? Storage = null,
    UpdatePerformanceConfigDto? Performance = null,
    UpdateAiAgentConfigDto? AiAgent = null);

/// <summary>
/// General settings DTO
/// </summary>
public record UpdateGeneralConfigDto(
    string? SiteName = null,
    string? SiteUrl = null,
    string? AdminEmail = null,
    string? DefaultLocale = null,
    string? DefaultTimezone = null);

/// <summary>
/// Auth settings DTO
/// </summary>
public record UpdateAuthConfigDto(
    int? SessionTimeout = null,
    bool? StrongPassword = null,
    int? PasswordMinLength = null,
    bool? TwoFactorAuth = null,
    bool? SsoEnabled = null,
    int? MaxFailedLogins = null,
    int? LockoutDuration = null);

/// <summary>
/// Storage settings DTO (storageUsed/limit read only)
/// </summary>
public record UpdateStorageConfigDto(
    int? MaxUploadSize = null);

/// <summary>
/// Performance settings DTO
/// </summary>
public record UpdatePerformanceConfigDto(
    bool? CacheEnabled = null,
    int? CacheTtlSeconds = null,
    bool? CdnEnabled = null,
    bool? CompressionEnabled = null,
    bool? RateLimitEnabled = null,
    int? RateLimitPerMinute = null);

/// <summary>
/// AI Agent settings DTO
/// </summary>
public record UpdateAiAgentConfigDto(
    bool? Enabled = null,
    string? Provider = null,
    string? Model = null,
    string? Endpoint = null,
    string? ApiKey = null,
    int? MaxTokens = null,
    decimal? Temperature = null);
