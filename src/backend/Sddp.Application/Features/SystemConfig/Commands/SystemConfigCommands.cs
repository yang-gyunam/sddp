using MediatR;
using Microsoft.Extensions.Logging;
using Sddp.Abstractions.DTOs;
using Sddp.Abstractions.Interfaces;
using Sddp.Abstractions.ValueObjects;
using Sddp.Application.Requests;
using SystemConfigEntity = Sddp.Domain.Entities.SystemConfig;

namespace Sddp.Application.Features.SystemConfig.Commands;

/// <summary>
/// system settings
/// </summary>
public sealed record SetSystemConfigValueCommand(
    string ConfigGroup,
    string ConfigKey,
    string Value,
    GlobalUniqueId? TenantId,
    GlobalUniqueId? ProjectId,
    GlobalUniqueId? UpdatedBy) : ICommand<SystemConfigItemDto?>, IAuditableRequest<SystemConfigItemDto?>
{
    public AuditLogRequest? AuditLog { get; set; }
    public AuditLogRequest? GetAuditLogRequest(SystemConfigItemDto? response) => AuditLog;
}

public sealed class SetSystemConfigValueCommandHandler : IRequestHandler<SetSystemConfigValueCommand, SystemConfigItemDto?>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<SetSystemConfigValueCommandHandler> _logger;

    public SetSystemConfigValueCommandHandler(
        IUnitOfWork unitOfWork,
        ILogger<SetSystemConfigValueCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<SystemConfigItemDto?> Handle(SetSystemConfigValueCommand request, CancellationToken cancellationToken)
    {
        var config = await (UpsertConfigValueAsync(request, cancellationToken)).ConfigureAwait(false);
        if (config is not null)
        {
            request.AuditLog = new AuditLogRequest(
                ActorId: request.UpdatedBy,
                Action: "set_value",
                ResourceType: "system_config",
                ResourceId: config.Id,
                Payload: new { request.ConfigGroup, request.ConfigKey },
                TenantId: request.TenantId,
                ProjectId: request.ProjectId);
        }
        return config is null ? null : SystemConfigHelpers.ToItemDto(config);
    }

    private async Task<SystemConfigEntity?> UpsertConfigValueAsync(SetSystemConfigValueCommand request, CancellationToken cancellationToken)
    {
        var repo = _unitOfWork.Repository<SystemConfigEntity>();

        var existing = await (SystemConfigHelpers.FindExactConfigAsync(
            _unitOfWork,
            request.ConfigGroup,
            request.ConfigKey,
            request.TenantId,
            request.ProjectId,
            includeInactive: true,
            cancellationToken)).ConfigureAwait(false);

        if (existing != null)
        {
            if (existing.IsReadonly)
            {
                _logger.LogWarning(
                    "Attempted to update readonly config: {Group}.{Key}",
                    request.ConfigGroup,
                    request.ConfigKey);
                return null;
            }

            if (!existing.IsActive)
            {
                existing.Activate();
            }

            existing.UpdateValue(request.Value, request.UpdatedBy);
            await (repo.UpdateAsync(existing, cancellationToken)).ConfigureAwait(false);
        }
        else
        {
            var parentConfig = await (SystemConfigHelpers.GetParentConfigAsync(
                _unitOfWork,
                request.ConfigGroup,
                request.ConfigKey,
                request.TenantId,
                request.ProjectId,
                cancellationToken)).ConfigureAwait(false);

            existing = new SystemConfigEntity
            {
                TenantId = request.TenantId,
                ProjectId = request.ProjectId,
                ConfigGroup = request.ConfigGroup,
                ConfigKey = request.ConfigKey,
                ConfigValue = request.Value,
                ValueType = parentConfig?.ValueType ?? "string",
                DisplayName = parentConfig?.DisplayName,
                Description = parentConfig?.Description,
                IsSensitive = parentConfig?.IsSensitive ?? false,
                IsReadonly = false,
                IsSystem = false,
                SortOrder = parentConfig?.SortOrder ?? 0,
                CreatedBy = request.UpdatedBy,
                UpdatedBy = request.UpdatedBy
            };

            await (repo.AddAsync(existing, cancellationToken)).ConfigureAwait(false);
        }

        await (_unitOfWork.SaveChangesAsync(cancellationToken)).ConfigureAwait(false);
        return existing;
    }
}

/// <summary>
/// system settings
/// </summary>
public sealed record SetSystemConfigGroupCommand(
    string ConfigGroup,
    Dictionary<string, string> Values,
    GlobalUniqueId? TenantId,
    GlobalUniqueId? ProjectId,
    GlobalUniqueId? UpdatedBy) : ICommand<bool>, IAuditableRequest<bool>
{
    public AuditLogRequest? AuditLog { get; set; }
    public AuditLogRequest? GetAuditLogRequest(bool response) => AuditLog;
}

public sealed class SetSystemConfigGroupCommandHandler : IRequestHandler<SetSystemConfigGroupCommand, bool>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<SetSystemConfigGroupCommandHandler> _logger;

    public SetSystemConfigGroupCommandHandler(
        IUnitOfWork unitOfWork,
        ILogger<SetSystemConfigGroupCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<bool> Handle(SetSystemConfigGroupCommand request, CancellationToken cancellationToken)
    {
        foreach (var (key, value) in request.Values)
        {
            await (UpsertConfigValueAsync(request, key, value, cancellationToken)).ConfigureAwait(false);
        }

        request.AuditLog = new AuditLogRequest(
            ActorId: request.UpdatedBy,
            Action: "set_group",
            ResourceType: "system_config",
            ResourceId: GlobalUniqueId.NewId(),
            Payload: new { request.ConfigGroup, KeyCount = request.Values.Count },
            TenantId: request.TenantId,
            ProjectId: request.ProjectId);

        return true;
    }

    private async Task<SystemConfigEntity?> UpsertConfigValueAsync(
        SetSystemConfigGroupCommand request,
        string configKey,
        string value,
        CancellationToken cancellationToken)
    {
        var repo = _unitOfWork.Repository<SystemConfigEntity>();

        var existing = await (SystemConfigHelpers.FindExactConfigAsync(
            _unitOfWork,
            request.ConfigGroup,
            configKey,
            request.TenantId,
            request.ProjectId,
            includeInactive: true,
            cancellationToken)).ConfigureAwait(false);

        if (existing != null)
        {
            if (existing.IsReadonly)
            {
                _logger.LogWarning(
                    "Attempted to update readonly config: {Group}.{Key}",
                    request.ConfigGroup,
                    configKey);
                return null;
            }

            if (!existing.IsActive)
            {
                existing.Activate();
            }

            existing.UpdateValue(value, request.UpdatedBy);
            await (repo.UpdateAsync(existing, cancellationToken)).ConfigureAwait(false);
        }
        else
        {
            var parentConfig = await (SystemConfigHelpers.GetParentConfigAsync(
                _unitOfWork,
                request.ConfigGroup,
                configKey,
                request.TenantId,
                request.ProjectId,
                cancellationToken)).ConfigureAwait(false);

            existing = new SystemConfigEntity
            {
                TenantId = request.TenantId,
                ProjectId = request.ProjectId,
                ConfigGroup = request.ConfigGroup,
                ConfigKey = configKey,
                ConfigValue = value,
                ValueType = parentConfig?.ValueType ?? "string",
                DisplayName = parentConfig?.DisplayName,
                Description = parentConfig?.Description,
                IsSensitive = parentConfig?.IsSensitive ?? false,
                IsReadonly = false,
                IsSystem = false,
                SortOrder = parentConfig?.SortOrder ?? 0,
                CreatedBy = request.UpdatedBy,
                UpdatedBy = request.UpdatedBy
            };

            await (repo.AddAsync(existing, cancellationToken)).ConfigureAwait(false);
        }

        await (_unitOfWork.SaveChangesAsync(cancellationToken)).ConfigureAwait(false);
        return existing;
    }
}

/// <summary>
/// system settings all
/// </summary>
public sealed record SaveSystemConfigCommand(
    UpdateSystemConfigDto Dto,
    GlobalUniqueId? TenantId,
    GlobalUniqueId? ProjectId,
    GlobalUniqueId? UpdatedBy) : ICommand<bool>, IAuditableRequest<bool>
{
    public AuditLogRequest? AuditLog { get; set; }
    public AuditLogRequest? GetAuditLogRequest(bool response) => AuditLog;
}

public sealed class SaveSystemConfigCommandHandler : IRequestHandler<SaveSystemConfigCommand, bool>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<SaveSystemConfigCommandHandler> _logger;

    public SaveSystemConfigCommandHandler(
        IUnitOfWork unitOfWork,
        ILogger<SaveSystemConfigCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<bool> Handle(SaveSystemConfigCommand request, CancellationToken cancellationToken)
    {
        if (request.Dto.General != null)
        {
            var values = new Dictionary<string, string>();
            if (request.Dto.General.SiteName != null || request.Dto.General.SiteUrl != null)
            {
                _logger.LogWarning("Readonly general fields (siteName/siteUrl) were provided and ignored in SaveSystemConfig.");
            }
            if (request.Dto.General.AdminEmail != null) values["adminEmail"] = request.Dto.General.AdminEmail;
            if (request.Dto.General.DefaultLocale != null) values["defaultLocale"] = request.Dto.General.DefaultLocale;
            if (request.Dto.General.DefaultTimezone != null) values["defaultTimezone"] = request.Dto.General.DefaultTimezone;
            await (SetConfigGroupAsync("general", values, request, cancellationToken)).ConfigureAwait(false);
        }

        if (request.Dto.Auth != null)
        {
            var values = new Dictionary<string, string>();
            if (request.Dto.Auth.SessionTimeout != null) values["sessionTimeout"] = request.Dto.Auth.SessionTimeout.Value.ToString();
            if (request.Dto.Auth.StrongPassword != null) values["strongPassword"] = request.Dto.Auth.StrongPassword.Value.ToString().ToLower();
            if (request.Dto.Auth.PasswordMinLength != null) values["passwordMinLength"] = request.Dto.Auth.PasswordMinLength.Value.ToString();
            if (request.Dto.Auth.TwoFactorAuth != null) values["twoFactorAuth"] = request.Dto.Auth.TwoFactorAuth.Value.ToString().ToLower();
            if (request.Dto.Auth.SsoEnabled != null) values["ssoEnabled"] = request.Dto.Auth.SsoEnabled.Value.ToString().ToLower();
            if (request.Dto.Auth.MaxFailedLogins != null) values["maxFailedLogins"] = request.Dto.Auth.MaxFailedLogins.Value.ToString();
            if (request.Dto.Auth.LockoutDuration != null) values["lockoutDuration"] = request.Dto.Auth.LockoutDuration.Value.ToString();
            await (SetConfigGroupAsync("auth", values, request, cancellationToken)).ConfigureAwait(false);
        }

        if (request.Dto.Storage != null)
        {
            var values = new Dictionary<string, string>();
            if (request.Dto.Storage.MaxUploadSize != null) values["maxUploadSize"] = request.Dto.Storage.MaxUploadSize.Value.ToString();
            await (SetConfigGroupAsync("storage", values, request, cancellationToken)).ConfigureAwait(false);
        }

        if (request.Dto.Performance != null)
        {
            var values = new Dictionary<string, string>();
            if (request.Dto.Performance.CacheEnabled != null) values["cacheEnabled"] = request.Dto.Performance.CacheEnabled.Value.ToString().ToLower();
            if (request.Dto.Performance.CacheTtlSeconds != null) values["cacheTtlSeconds"] = request.Dto.Performance.CacheTtlSeconds.Value.ToString();
            if (request.Dto.Performance.CdnEnabled != null) values["cdnEnabled"] = request.Dto.Performance.CdnEnabled.Value.ToString().ToLower();
            if (request.Dto.Performance.CompressionEnabled != null) values["compressionEnabled"] = request.Dto.Performance.CompressionEnabled.Value.ToString().ToLower();
            if (request.Dto.Performance.RateLimitEnabled != null) values["rateLimitEnabled"] = request.Dto.Performance.RateLimitEnabled.Value.ToString().ToLower();
            if (request.Dto.Performance.RateLimitPerMinute != null) values["rateLimitPerMinute"] = request.Dto.Performance.RateLimitPerMinute.Value.ToString();
            await (SetConfigGroupAsync("performance", values, request, cancellationToken)).ConfigureAwait(false);
        }

        if (request.Dto.AiAgent != null)
        {
            var values = new Dictionary<string, string>();
            if (request.Dto.AiAgent.Enabled != null) values["enabled"] = request.Dto.AiAgent.Enabled.Value.ToString().ToLower();
            if (request.Dto.AiAgent.Provider != null) values["provider"] = request.Dto.AiAgent.Provider;
            if (request.Dto.AiAgent.Model != null) values["model"] = request.Dto.AiAgent.Model;
            if (request.Dto.AiAgent.Endpoint != null) values["endpoint"] = request.Dto.AiAgent.Endpoint;
            if (request.Dto.AiAgent.ApiKey != null) values["apiKey"] = request.Dto.AiAgent.ApiKey;
            if (request.Dto.AiAgent.MaxTokens != null) values["maxTokens"] = request.Dto.AiAgent.MaxTokens.Value.ToString();
            if (request.Dto.AiAgent.Temperature != null) values["temperature"] = request.Dto.AiAgent.Temperature.Value.ToString();
            await (SetConfigGroupAsync("aiAgent", values, request, cancellationToken)).ConfigureAwait(false);
        }

        // Build changed groups summary for audit
        var changedGroups = new List<string>();
        if (request.Dto.General != null) changedGroups.Add("general");
        if (request.Dto.Auth != null) changedGroups.Add("auth");
        if (request.Dto.Storage != null) changedGroups.Add("storage");
        if (request.Dto.Performance != null) changedGroups.Add("performance");
        if (request.Dto.AiAgent != null) changedGroups.Add("aiAgent");

        request.AuditLog = new AuditLogRequest(
            ActorId: request.UpdatedBy,
            Action: "save",
            ResourceType: "system_config",
            ResourceId: GlobalUniqueId.NewId(),
            Payload: new { ChangedGroups = changedGroups, GroupCount = changedGroups.Count },
            TenantId: request.TenantId,
            ProjectId: request.ProjectId);

        return true;
    }

    private async Task SetConfigGroupAsync(
        string group,
        Dictionary<string, string> values,
        SaveSystemConfigCommand request,
        CancellationToken cancellationToken)
    {
        foreach (var (key, value) in values)
        {
            await (UpsertConfigValueAsync(group, key, value, request, cancellationToken)).ConfigureAwait(false);
        }
    }

    private async Task<SystemConfigEntity?> UpsertConfigValueAsync(
        string configGroup,
        string configKey,
        string value,
        SaveSystemConfigCommand request,
        CancellationToken cancellationToken)
    {
        var repo = _unitOfWork.Repository<SystemConfigEntity>();

        var existing = await (SystemConfigHelpers.FindExactConfigAsync(
            _unitOfWork,
            configGroup,
            configKey,
            request.TenantId,
            request.ProjectId,
            includeInactive: true,
            cancellationToken)).ConfigureAwait(false);

        if (existing != null)
        {
            if (existing.IsReadonly)
            {
                _logger.LogWarning(
                    "Attempted to update readonly config: {Group}.{Key}",
                    configGroup,
                    configKey);
                return null;
            }

            if (!existing.IsActive)
            {
                existing.Activate();
            }

            existing.UpdateValue(value, request.UpdatedBy);
            await (repo.UpdateAsync(existing, cancellationToken)).ConfigureAwait(false);
        }
        else
        {
            var parentConfig = await (SystemConfigHelpers.GetParentConfigAsync(
                _unitOfWork,
                configGroup,
                configKey,
                request.TenantId,
                request.ProjectId,
                cancellationToken)).ConfigureAwait(false);

            existing = new SystemConfigEntity
            {
                TenantId = request.TenantId,
                ProjectId = request.ProjectId,
                ConfigGroup = configGroup,
                ConfigKey = configKey,
                ConfigValue = value,
                ValueType = parentConfig?.ValueType ?? "string",
                DisplayName = parentConfig?.DisplayName,
                Description = parentConfig?.Description,
                IsSensitive = parentConfig?.IsSensitive ?? false,
                IsReadonly = false,
                IsSystem = false,
                SortOrder = parentConfig?.SortOrder ?? 0,
                CreatedBy = request.UpdatedBy,
                UpdatedBy = request.UpdatedBy
            };

            await (repo.AddAsync(existing, cancellationToken)).ConfigureAwait(false);
        }

        await (_unitOfWork.SaveChangesAsync(cancellationToken)).ConfigureAwait(false);
        return existing;
    }
}

/// <summary>
/// system settings delete
/// </summary>
public sealed record DeleteSystemConfigValueCommand(
    string ConfigGroup,
    string ConfigKey,
    GlobalUniqueId? TenantId,
    GlobalUniqueId? ProjectId) : ICommand<bool>, IAuditableRequest<bool>
{
    public AuditLogRequest? AuditLog { get; set; }
    public AuditLogRequest? GetAuditLogRequest(bool response) => response ? AuditLog : null;
}

public sealed class DeleteSystemConfigValueCommandHandler : IRequestHandler<DeleteSystemConfigValueCommand, bool>
{
    private readonly IUnitOfWork _unitOfWork;

    public DeleteSystemConfigValueCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<bool> Handle(DeleteSystemConfigValueCommand request, CancellationToken cancellationToken)
    {
        var repo = _unitOfWork.Repository<SystemConfigEntity>();
        var config = await (SystemConfigHelpers.FindExactConfigAsync(
            _unitOfWork,
            request.ConfigGroup,
            request.ConfigKey,
            request.TenantId,
            request.ProjectId,
            cancellationToken)).ConfigureAwait(false);

        if (config == null || config.IsSystem)
        {
            return false;
        }

        await (repo.DeleteAsync(config, cancellationToken)).ConfigureAwait(false);
        await (_unitOfWork.SaveChangesAsync(cancellationToken)).ConfigureAwait(false);

        request.AuditLog = new AuditLogRequest(
            ActorId: null,
            Action: "delete_value",
            ResourceType: "system_config",
            ResourceId: config.Id,
            Payload: new { request.ConfigGroup, request.ConfigKey },
            TenantId: request.TenantId,
            ProjectId: request.ProjectId);

        return true;
    }
}

/// <summary>
/// system settings
/// </summary>
public sealed record ResetSystemConfigCommand(
    GlobalUniqueId? TenantId,
    GlobalUniqueId? ProjectId) : ICommand<bool>, IAuditableRequest<bool>
{
    public AuditLogRequest? AuditLog { get; set; }
    public AuditLogRequest? GetAuditLogRequest(bool response) => AuditLog;
}

public sealed class ResetSystemConfigCommandHandler : IRequestHandler<ResetSystemConfigCommand, bool>
{
    private readonly IUnitOfWork _unitOfWork;

    public ResetSystemConfigCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<bool> Handle(ResetSystemConfigCommand request, CancellationToken cancellationToken)
    {
        var repo = _unitOfWork.Repository<SystemConfigEntity>();

        var configs = await (repo.FindAsync(
            c => c.TenantId == request.TenantId
                && c.ProjectId == request.ProjectId
                && !c.IsSystem
                && c.IsActive,
            cancellationToken)).ConfigureAwait(false);

        foreach (var config in configs)
        {
            await (repo.DeleteAsync(config, cancellationToken)).ConfigureAwait(false);
        }

        await (_unitOfWork.SaveChangesAsync(cancellationToken)).ConfigureAwait(false);

        request.AuditLog = new AuditLogRequest(
            ActorId: null,
            Action: "reset",
            ResourceType: "system_config",
            ResourceId: GlobalUniqueId.NewId(),
            Payload: new { DeletedCount = configs.Count() },
            TenantId: request.TenantId,
            ProjectId: request.ProjectId);

        return true;
    }
}
