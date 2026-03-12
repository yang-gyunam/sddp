using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Sddp.Abstractions.DTOs;
using Sddp.Api.Constants;
using Sddp.Application.Features.SystemConfig.Commands;
using Sddp.Application.Features.SystemConfig.Queries;

namespace Sddp.Api.Controllers;

/// <summary>
/// System configuration controller
/// </summary>
[Route("api/system/config")]
[Authorize]
public class SystemConfigController : BaseApiController
{
    private static readonly HashSet<string> ReadonlyGeneralKeys = ["siteName", "siteUrl"];
    private static readonly HashSet<string> ReadonlyStorageKeys = ["database", "storageUsed", "storageLimit"];
    private readonly ISender _sender;
    private readonly ILogger<SystemConfigController> _logger;
    private readonly bool _isAuthenticationConfigEnabled;
    private readonly bool _isPerformanceConfigEnabled;
    private readonly string _authenticationConfigDisabledMessage;
    private readonly string _performanceConfigDisabledMessage;

    public SystemConfigController(
        ISender sender,
        ILogger<SystemConfigController> logger,
        IConfiguration configuration)
    {
        _sender = sender;
        _logger = logger;
        _isAuthenticationConfigEnabled = configuration.GetValue("FeatureFlags:SystemConfig:AuthenticationEnabled", false);
        _isPerformanceConfigEnabled = configuration.GetValue("FeatureFlags:SystemConfig:PerformanceEnabled", false);
        _authenticationConfigDisabledMessage = configuration.GetValue<string>("FeatureFlags:SystemConfig:AuthenticationDisabledMessage")
            ?? "Authentication settings are coming soon.";
        _performanceConfigDisabledMessage = configuration.GetValue<string>("FeatureFlags:SystemConfig:PerformanceDisabledMessage")
            ?? "Performance settings are coming soon.";
    }

    private bool IsConfigGroupEnabled(string group)
    {
        return group switch
        {
            "auth" => _isAuthenticationConfigEnabled,
            "performance" => _isPerformanceConfigEnabled,
            _ => true
        };
    }

    private IActionResult ConfigGroupDisabled(string group)
    {
        var message = group switch
        {
            "auth" => _authenticationConfigDisabledMessage,
            "performance" => _performanceConfigDisabledMessage,
            _ => "This config group is currently disabled."
        };

        return StatusCode(
            StatusCodes.Status503ServiceUnavailable,
            ApiResponse<object>.Fail("SYSTEM_CONFIG_GROUP_DISABLED", message));
    }

    private UpdateSystemConfigDto ApplyFeatureFlags(UpdateSystemConfigDto dto)
    {
        return dto with
        {
            Auth = _isAuthenticationConfigEnabled ? dto.Auth : null,
            Performance = _isPerformanceConfigEnabled ? dto.Performance : null
        };
    }

    private bool HasAnyEnabledGroup(UpdateSystemConfigDto dto)
    {
        return dto.General is not null
               || dto.Storage is not null
               || dto.AiAgent is not null
               || (_isAuthenticationConfigEnabled && dto.Auth is not null)
               || (_isPerformanceConfigEnabled && dto.Performance is not null);
    }

    private static bool IsReadonlyGeneralKey(string key)
    {
        return ReadonlyGeneralKeys.Contains(key);
    }

    private static bool IsReadonlyStorageKey(string key)
    {
        return ReadonlyStorageKeys.Contains(key);
    }

    private static IActionResult ReadonlyGeneralFieldError()
    {
        return new BadRequestObjectResult(
            ApiResponse<object>.Fail(
                ApiErrorCodes.Validation.InvalidRequest,
                "general.siteName and general.siteUrl are readonly fields."));
    }

    private static IActionResult ReadonlyStorageFieldError()
    {
        return new BadRequestObjectResult(
            ApiResponse<object>.Fail(
                ApiErrorCodes.Validation.InvalidRequest,
                "storage.database, storage.storageUsed and storage.storageLimit are readonly fields."));
    }

    /// <summary>
    /// Gets the complete system configuration
    /// </summary>
    /// <remarks>
    /// Includes tenant-level settings when the X-Tenant-Id header is present.
    /// Includes project-level settings when the X-Project-Id header is present.
    /// Lower scopes override higher-scope values.
    /// </remarks>
    [HttpGet]
    [Authorize(Policy = "CanManageUsers")]
    public async Task<IActionResult> GetConfig(CancellationToken cancellationToken = default)
    {
        TryGetTenantId(out var tenantId);
        TryGetProjectId(out var projectId);

        var config = await _sender.Send(new GetSystemConfigQuery(
            tenantId.ToGuid() != Guid.Empty ? tenantId : null,
            projectId.ToGuid() != Guid.Empty ? projectId : null), cancellationToken);

        return Ok(ApiResponse<SystemConfigDto>.Ok(config));
    }

    /// <summary>
    /// Gets settings by configuration group
    /// </summary>
    /// <param name="group">Configuration group (general, auth, storage, performance, aiAgent)</param>
    [HttpGet("groups/{group}")]
    [Authorize(Policy = "CanManageUsers")]
    public async Task<IActionResult> GetConfigGroup(string group, CancellationToken cancellationToken = default)
    {
        var validGroups = new[] { "general", "auth", "storage", "performance", "aiAgent" };
        if (!validGroups.Contains(group))
        {
            return BadRequest(ApiResponse<object>.Fail(ApiErrorCodes.Validation.InvalidGroup, $"Invalid config group: {group}. Valid groups: {string.Join(", ", validGroups)}"));
        }

        if (!IsConfigGroupEnabled(group))
        {
            return ConfigGroupDisabled(group);
        }

        TryGetTenantId(out var tenantId);
        TryGetProjectId(out var projectId);

        var configs = await _sender.Send(new GetSystemConfigGroupQuery(
            group,
            tenantId.ToGuid() != Guid.Empty ? tenantId : null,
            projectId.ToGuid() != Guid.Empty ? projectId : null), cancellationToken);

        return Ok(ApiResponse<Dictionary<string, SystemConfigItemDto>>.Ok(configs));
    }

    /// <summary>
    /// Gets a single configuration value
    /// </summary>
    /// <param name="group">Configuration group</param>
    /// <param name="key">Configuration key</param>
    [HttpGet("groups/{group}/{key}")]
    [Authorize(Policy = "CanManageUsers")]
    public async Task<IActionResult> GetConfigValue(string group, string key, CancellationToken cancellationToken = default)
    {
        if (!IsConfigGroupEnabled(group))
        {
            return ConfigGroupDisabled(group);
        }

        TryGetTenantId(out var tenantId);
        TryGetProjectId(out var projectId);

        var config = await _sender.Send(new GetSystemConfigValueQuery(
            group,
            key,
            tenantId.ToGuid() != Guid.Empty ? tenantId : null,
            projectId.ToGuid() != Guid.Empty ? projectId : null), cancellationToken);

        if (config == null)
        {
            return NotFound(ApiResponse<object>.Fail(ApiErrorCodes.Crud.NotFound, $"Config not found: {group}.{key}"));
        }

        return Ok(ApiResponse<SystemConfigItemDto>.Ok(config));
    }

    /// <summary>
    /// Saves the complete system configuration
    /// </summary>
    [HttpPut]
    [Authorize(Policy = "CanManageUsers")]
    public async Task<IActionResult> SaveConfig([FromBody] UpdateSystemConfigDto dto, CancellationToken cancellationToken = default)
    {
        if (dto.General?.SiteName is not null || dto.General?.SiteUrl is not null)
        {
            return ReadonlyGeneralFieldError();
        }

        if (!HasAnyEnabledGroup(dto))
        {
            if (dto.Auth is not null && !_isAuthenticationConfigEnabled)
            {
                return ConfigGroupDisabled("auth");
            }

            if (dto.Performance is not null && !_isPerformanceConfigEnabled)
            {
                return ConfigGroupDisabled("performance");
            }
        }

        if (dto.Auth is not null && !_isAuthenticationConfigEnabled)
        {
            _logger.LogInformation("Ignoring disabled auth config payload in SaveConfig");
        }

        if (dto.Performance is not null && !_isPerformanceConfigEnabled)
        {
            _logger.LogInformation("Ignoring disabled performance config payload in SaveConfig");
        }

        var effectiveDto = ApplyFeatureFlags(dto);

        var userError = RequireUserId(
            out var userId,
            unauthorizedIfMissing: true,
            errorCode: ApiErrorCodes.Auth.Unauthorized,
            errorMessage: "User not authenticated");
        if (userError is not null)
        {
            return userError;
        }

        TryGetTenantId(out var tenantId);
        TryGetProjectId(out var projectId);

        var success = await _sender.Send(new SaveSystemConfigCommand(
            effectiveDto,
            tenantId.ToGuid() != Guid.Empty ? tenantId : null,
            projectId.ToGuid() != Guid.Empty ? projectId : null,
            userId), cancellationToken);

        if (!success)
        {
            return BadRequest(ApiResponse<object>.Fail(ApiErrorCodes.Crud.SaveFailed, "Failed to save configuration"));
        }

        // Return the latest configuration after saving
        var config = await _sender.Send(new GetSystemConfigQuery(
            tenantId.ToGuid() != Guid.Empty ? tenantId : null,
            projectId.ToGuid() != Guid.Empty ? projectId : null), cancellationToken);

        _logger.LogInformation("System config updated by user {UserId}", userId);

        return Ok(ApiResponse<SystemConfigDto>.Ok(config));
    }

    /// <summary>
    /// Saves a configuration group in bulk
    /// </summary>
    /// <param name="group">Configuration group (general, auth, storage, performance, aiAgent)</param>
    /// <param name="request">Dictionary of values in the group</param>
    [HttpPut("groups/{group}")]
    [Authorize(Policy = "CanManageUsers")]
    public async Task<IActionResult> SetConfigGroup(string group, [FromBody] SetConfigGroupRequest request, CancellationToken cancellationToken = default)
    {
        var validGroups = new[] { "general", "auth", "storage", "performance", "aiAgent" };
        if (!validGroups.Contains(group))
        {
            return BadRequest(ApiResponse<object>.Fail(ApiErrorCodes.Validation.InvalidGroup, $"Invalid config group: {group}. Valid groups: {string.Join(", ", validGroups)}"));
        }

        if (!IsConfigGroupEnabled(group))
        {
            return ConfigGroupDisabled(group);
        }

        if (group == "general" && request.Values.Keys.Any(IsReadonlyGeneralKey))
        {
            return ReadonlyGeneralFieldError();
        }

        if (group == "storage" && request.Values.Keys.Any(IsReadonlyStorageKey))
        {
            return ReadonlyStorageFieldError();
        }

        var userError = RequireUserId(
            out var userId,
            unauthorizedIfMissing: true,
            errorCode: ApiErrorCodes.Auth.Unauthorized,
            errorMessage: "User not authenticated");
        if (userError is not null)
        {
            return userError;
        }

        TryGetTenantId(out var tenantId);
        TryGetProjectId(out var projectId);

        var success = await _sender.Send(new SetSystemConfigGroupCommand(
            group,
            request.Values,
            tenantId.ToGuid() != Guid.Empty ? tenantId : null,
            projectId.ToGuid() != Guid.Empty ? projectId : null,
            userId), cancellationToken);

        if (!success)
        {
            return BadRequest(ApiResponse<object>.Fail(ApiErrorCodes.Crud.SaveFailed, $"Failed to save config group: {group}"));
        }

        // Return the latest group settings after saving
        var configs = await _sender.Send(new GetSystemConfigGroupQuery(
            group,
            tenantId.ToGuid() != Guid.Empty ? tenantId : null,
            projectId.ToGuid() != Guid.Empty ? projectId : null), cancellationToken);

        _logger.LogInformation("Config group '{Group}' updated by user {UserId}", group, userId);

        return Ok(ApiResponse<Dictionary<string, SystemConfigItemDto>>.Ok(configs));
    }

    /// <summary>
    /// Saves a single configuration value
    /// </summary>
    /// <param name="group">Configuration group</param>
    /// <param name="key">Configuration key</param>
    /// <param name="request">Configuration value</param>
    [HttpPut("groups/{group}/{key}")]
    [Authorize(Policy = "CanManageUsers")]
    public async Task<IActionResult> SetConfigValue(string group, string key, [FromBody] SetConfigValueRequest request, CancellationToken cancellationToken = default)
    {
        if (!IsConfigGroupEnabled(group))
        {
            return ConfigGroupDisabled(group);
        }

        if (group == "general" && IsReadonlyGeneralKey(key))
        {
            return ReadonlyGeneralFieldError();
        }

        if (group == "storage" && IsReadonlyStorageKey(key))
        {
            return ReadonlyStorageFieldError();
        }

        var userError = RequireUserId(
            out var userId,
            unauthorizedIfMissing: true,
            errorCode: ApiErrorCodes.Auth.Unauthorized,
            errorMessage: "User not authenticated");
        if (userError is not null)
        {
            return userError;
        }

        TryGetTenantId(out var tenantId);
        TryGetProjectId(out var projectId);

        var config = await _sender.Send(new SetSystemConfigValueCommand(
            group,
            key,
            request.Value,
            tenantId.ToGuid() != Guid.Empty ? tenantId : null,
            projectId.ToGuid() != Guid.Empty ? projectId : null,
            userId), cancellationToken);

        if (config == null)
        {
            return BadRequest(ApiResponse<object>.Fail(ApiErrorCodes.Crud.SetFailed, $"Failed to set config: {group}.{key}. It may be readonly."));
        }

        return Ok(ApiResponse<SystemConfigItemDto>.Ok(config));
    }

    /// <summary>
    /// Deletes a configuration value and falls back to the parent scope
    /// </summary>
    /// <param name="group">Configuration group</param>
    /// <param name="key">Configuration key</param>
    [HttpDelete("groups/{group}/{key}")]
    [Authorize(Policy = "CanManageUsers")]
    public async Task<IActionResult> DeleteConfigValue(string group, string key, CancellationToken cancellationToken = default)
    {
        if (!IsConfigGroupEnabled(group))
        {
            return ConfigGroupDisabled(group);
        }

        TryGetTenantId(out var tenantId);
        TryGetProjectId(out var projectId);

        var success = await _sender.Send(new DeleteSystemConfigValueCommand(
            group,
            key,
            tenantId.ToGuid() != Guid.Empty ? tenantId : null,
            projectId.ToGuid() != Guid.Empty ? projectId : null), cancellationToken);

        if (!success)
        {
            return BadRequest(ApiResponse<object>.Fail(ApiErrorCodes.Crud.DeleteFailed, $"Failed to delete config: {group}.{key}. It may be a system config."));
        }

        return Ok(ApiResponse.Ok());
    }

    /// <summary>
    /// Resets settings to defaults
    /// </summary>
    [HttpPost("reset")]
    [Authorize(Policy = "CanManageUsers")]
    public async Task<IActionResult> ResetToDefault(CancellationToken cancellationToken = default)
    {
        TryGetTenantId(out var tenantId);
        TryGetProjectId(out var projectId);

        var success = await _sender.Send(new ResetSystemConfigCommand(
            tenantId.ToGuid() != Guid.Empty ? tenantId : null,
            projectId.ToGuid() != Guid.Empty ? projectId : null), cancellationToken);

        if (!success)
        {
            return BadRequest(ApiResponse<object>.Fail(ApiErrorCodes.Crud.SetFailed, "Failed to reset configuration"));
        }

        // Return the latest configuration after the reset
        var config = await _sender.Send(new GetSystemConfigQuery(
            tenantId.ToGuid() != Guid.Empty ? tenantId : null,
            projectId.ToGuid() != Guid.Empty ? projectId : null), cancellationToken);

        return Ok(ApiResponse<SystemConfigDto>.Ok(config));
    }
}

/// <summary>
/// Request DTO for saving a configuration value
/// </summary>
public record SetConfigValueRequest(string Value);

/// <summary>
/// Request DTO for saving a configuration group in bulk
/// </summary>
public record SetConfigGroupRequest(Dictionary<string, string> Values);
