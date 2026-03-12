using Sddp.Abstractions.DTOs;
using Sddp.Abstractions.Interfaces;
using Sddp.Abstractions.ValueObjects;
using SystemConfigEntity = Sddp.Domain.Entities.SystemConfig;

namespace Sddp.Application.Features.SystemConfig;

internal static class SystemConfigHelpers
{
    internal static async Task<List<SystemConfigEntity>> GetAllConfigsWithFallbackAsync(
        IUnitOfWork unitOfWork,
        GlobalUniqueId? tenantId,
        GlobalUniqueId? projectId,
        CancellationToken cancellationToken)
    {
        var repo = unitOfWork.Repository<SystemConfigEntity>();

        var globalConfigs = await repo.FindAsync(
            c => c.TenantId == null && c.ProjectId == null && c.IsActive,
            cancellationToken).ConfigureAwait(false);

        var result = globalConfigs.ToList();

        if (tenantId != null)
        {
            var tenantConfigs = await repo.FindAsync(
                c => c.TenantId == tenantId && c.ProjectId == null && c.IsActive,
                cancellationToken).ConfigureAwait(false);

            foreach (var config in tenantConfigs)
            {
                var existing = result.FindIndex(c => c.ConfigGroup == config.ConfigGroup && c.ConfigKey == config.ConfigKey);
                if (existing >= 0)
                {
                    result[existing] = config;
                }
                else
                {
                    result.Add(config);
                }
            }
        }

        if (tenantId != null && projectId != null)
        {
            var projectConfigs = await repo.FindAsync(
                c => c.TenantId == tenantId && c.ProjectId == projectId && c.IsActive,
                cancellationToken).ConfigureAwait(false);

            foreach (var config in projectConfigs)
            {
                var existing = result.FindIndex(c => c.ConfigGroup == config.ConfigGroup && c.ConfigKey == config.ConfigKey);
                if (existing >= 0)
                {
                    result[existing] = config;
                }
                else
                {
                    result.Add(config);
                }
            }
        }

        return result;
    }

    internal static async Task<SystemConfigEntity?> FindExactConfigAsync(
        IUnitOfWork unitOfWork,
        string configGroup,
        string configKey,
        GlobalUniqueId? tenantId,
        GlobalUniqueId? projectId,
        bool includeInactive,
        CancellationToken cancellationToken)
    {
        var repo = unitOfWork.Repository<SystemConfigEntity>();
        var configs = includeInactive
            ? await (repo.FindIncludingInactiveAsync(
                c => c.ConfigGroup == configGroup
                    && c.ConfigKey == configKey
                    && c.TenantId == tenantId
                    && c.ProjectId == projectId,
                cancellationToken)).ConfigureAwait(false)
            : await (repo.FindAsync(
                c => c.ConfigGroup == configGroup
                    && c.ConfigKey == configKey
                    && c.TenantId == tenantId
                    && c.ProjectId == projectId,
                cancellationToken)).ConfigureAwait(false);

        return configs.FirstOrDefault();
    }

    internal static Task<SystemConfigEntity?> FindExactConfigAsync(
        IUnitOfWork unitOfWork,
        string configGroup,
        string configKey,
        GlobalUniqueId? tenantId,
        GlobalUniqueId? projectId,
        CancellationToken cancellationToken)
        => FindExactConfigAsync(
            unitOfWork,
            configGroup,
            configKey,
            tenantId,
            projectId,
            includeInactive: false,
            cancellationToken);

    internal static async Task<SystemConfigEntity?> GetParentConfigAsync(
        IUnitOfWork unitOfWork,
        string configGroup,
        string configKey,
        GlobalUniqueId? tenantId,
        GlobalUniqueId? projectId,
        CancellationToken cancellationToken)
    {
        if (projectId != null && tenantId != null)
        {
            var tenantConfig = await FindExactConfigAsync(
                unitOfWork,
                configGroup,
                configKey,
                tenantId,
                null,
                includeInactive: false,
                cancellationToken).ConfigureAwait(false);

            if (tenantConfig != null)
            {
                return tenantConfig;
            }
        }

        if (tenantId != null)
        {
            return await FindExactConfigAsync(
                unitOfWork,
                configGroup,
                configKey,
                null,
                null,
                includeInactive: false,
                cancellationToken).ConfigureAwait(false);
        }

        return null;
    }

    internal static SystemConfigItemDto ToItemDto(SystemConfigEntity config)
    {
        return new SystemConfigItemDto(
            ConfigGroup: config.ConfigGroup,
            ConfigKey: config.ConfigKey,
            Value: config.IsSensitive ? "***" : config.ConfigValue,
            ValueType: config.ValueType,
            DisplayName: config.DisplayName,
            Description: config.Description,
            IsSensitive: config.IsSensitive,
            IsReadonly: config.IsReadonly);
    }

    internal static SystemConfigDto BuildSystemConfig(IEnumerable<SystemConfigEntity> configs)
    {
        return new SystemConfigDto(
            General: BuildGeneralConfig(configs),
            Auth: BuildAuthConfig(configs),
            Storage: BuildStorageConfig(configs),
            Performance: BuildPerformanceConfig(configs),
            AiAgent: BuildAiAgentConfig(configs));
    }

    private static string GetConfigValue(IEnumerable<SystemConfigEntity> configs, string group, string key, string defaultValue = "")
    {
        var config = configs.FirstOrDefault(c => c.ConfigGroup == group && c.ConfigKey == key);
        return config?.ConfigValue ?? defaultValue;
    }

    internal static async Task<bool> IsAiEnabledAsync(
        IUnitOfWork unitOfWork,
        GlobalUniqueId tenantId,
        GlobalUniqueId? projectId,
        CancellationToken cancellationToken)
    {
        var configs = await GetAllConfigsWithFallbackAsync(unitOfWork, tenantId, projectId, cancellationToken)
            .ConfigureAwait(false);

        return GetBoolConfigValue(configs, "aiAgent", "enabled", false);
    }

    private static bool GetBoolConfigValue(IEnumerable<SystemConfigEntity> configs, string group, string key, bool defaultValue = false)
    {
        var value = GetConfigValue(configs, group, key);
        return bool.TryParse(value, out var result) ? result : defaultValue;
    }

    private static int GetIntConfigValue(IEnumerable<SystemConfigEntity> configs, string group, string key, int defaultValue = 0)
    {
        var value = GetConfigValue(configs, group, key);
        return int.TryParse(value, out var result) ? result : defaultValue;
    }

    private static decimal GetDecimalConfigValue(IEnumerable<SystemConfigEntity> configs, string group, string key, decimal defaultValue = 0)
    {
        var value = GetConfigValue(configs, group, key);
        return decimal.TryParse(value, out var result) ? result : defaultValue;
    }

    private static GeneralConfigDto BuildGeneralConfig(IEnumerable<SystemConfigEntity> configs)
    {
        return new GeneralConfigDto(
            SiteName: GetConfigValue(configs, "general", "siteName", "SDDP"),
            SiteUrl: GetConfigValue(configs, "general", "siteUrl", "https://sddp.example.com"),
            AdminEmail: GetConfigValue(configs, "general", "adminEmail", "admin@example.com"),
            DefaultLocale: GetConfigValue(configs, "general", "defaultLocale", "en-US"),
            DefaultTimezone: GetConfigValue(configs, "general", "defaultTimezone", "UTC"));
    }

    private static AuthConfigDto BuildAuthConfig(IEnumerable<SystemConfigEntity> configs)
    {
        return new AuthConfigDto(
            SessionTimeout: GetIntConfigValue(configs, "auth", "sessionTimeout", 30),
            StrongPassword: GetBoolConfigValue(configs, "auth", "strongPassword", true),
            PasswordMinLength: GetIntConfigValue(configs, "auth", "passwordMinLength", 8),
            TwoFactorAuth: GetBoolConfigValue(configs, "auth", "twoFactorAuth", false),
            SsoEnabled: GetBoolConfigValue(configs, "auth", "ssoEnabled", false),
            MaxFailedLogins: GetIntConfigValue(configs, "auth", "maxFailedLogins", 5),
            LockoutDuration: GetIntConfigValue(configs, "auth", "lockoutDuration", 15));
    }

    private static StorageConfigDto BuildStorageConfig(IEnumerable<SystemConfigEntity> configs)
    {
        return new StorageConfigDto(
            Database: GetConfigValue(configs, "storage", "database", "PostgreSQL"),
            StorageUsed: GetDecimalConfigValue(configs, "storage", "storageUsed", 0),
            StorageLimit: GetDecimalConfigValue(configs, "storage", "storageLimit", 0),
            MaxUploadSize: GetIntConfigValue(configs, "storage", "maxUploadSize", 50));
    }

    private static PerformanceConfigDto BuildPerformanceConfig(IEnumerable<SystemConfigEntity> configs)
    {
        return new PerformanceConfigDto(
            CacheEnabled: GetBoolConfigValue(configs, "performance", "cacheEnabled", true),
            CacheTtlSeconds: GetIntConfigValue(configs, "performance", "cacheTtlSeconds", 300),
            CdnEnabled: GetBoolConfigValue(configs, "performance", "cdnEnabled", false),
            CompressionEnabled: GetBoolConfigValue(configs, "performance", "compressionEnabled", true),
            RateLimitEnabled: GetBoolConfigValue(configs, "performance", "rateLimitEnabled", true),
            RateLimitPerMinute: GetIntConfigValue(configs, "performance", "rateLimitPerMinute", 100));
    }

    private static AiAgentConfigDto BuildAiAgentConfig(IEnumerable<SystemConfigEntity> configs)
    {
        var apiKeyConfig = configs.FirstOrDefault(c => c.ConfigGroup == "aiAgent" && c.ConfigKey == "apiKey");
        var apiKeyValue = apiKeyConfig?.IsSensitive == true ? "***" : apiKeyConfig?.ConfigValue;

        return new AiAgentConfigDto(
            Enabled: GetBoolConfigValue(configs, "aiAgent", "enabled", false),
            Provider: GetConfigValue(configs, "aiAgent", "provider", "Disabled"),
            Model: GetConfigValue(configs, "aiAgent", "model", string.Empty),
            Endpoint: GetConfigValue(configs, "aiAgent", "endpoint", string.Empty),
            ApiKey: string.IsNullOrEmpty(apiKeyValue) ? null : apiKeyValue,
            MaxTokens: GetIntConfigValue(configs, "aiAgent", "maxTokens", 0),
            Temperature: GetDecimalConfigValue(configs, "aiAgent", "temperature", 0));
    }
}
