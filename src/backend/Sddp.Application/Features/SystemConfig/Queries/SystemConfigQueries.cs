using MediatR;
using Dapper;
using System.Data;
using Sddp.Abstractions.DTOs;
using Sddp.Abstractions.Interfaces;
using Sddp.Abstractions.ValueObjects;
using Sddp.Application.Requests;
using Microsoft.Extensions.Logging;
using System.Globalization;

namespace Sddp.Application.Features.SystemConfig.Queries;

/// <summary>
/// system settings all get
/// </summary>
public sealed record GetSystemConfigQuery(
    GlobalUniqueId? TenantId,
    GlobalUniqueId? ProjectId) : IQuery<SystemConfigDto>;

public sealed class GetSystemConfigQueryHandler : IRequestHandler<GetSystemConfigQuery, SystemConfigDto>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDbConnection? _connection;
    private readonly ILogger<GetSystemConfigQueryHandler>? _logger;
    private const decimal BytesPerGiB = 1024m * 1024m * 1024m;

    public GetSystemConfigQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public GetSystemConfigQueryHandler(
        IUnitOfWork unitOfWork,
        IDbConnection connection,
        ILogger<GetSystemConfigQueryHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _connection = connection;
        _logger = logger;
    }

    public async Task<SystemConfigDto> Handle(GetSystemConfigQuery request, CancellationToken cancellationToken)
    {
        var configs = await (SystemConfigHelpers.GetAllConfigsWithFallbackAsync(
            _unitOfWork,
            request.TenantId,
            request.ProjectId,
            cancellationToken)).ConfigureAwait(false);

        var config = SystemConfigHelpers.BuildSystemConfig(configs);
        var runtimeStorage = await (TryGetRuntimeStorageMetricsAsync(cancellationToken)).ConfigureAwait(false);
        if (runtimeStorage == null)
        {
            return config;
        }

        var storageLimit = runtimeStorage.StorageLimitGiB > 0
            ? runtimeStorage.StorageLimitGiB
            : config.Storage.StorageLimit;
        var tablespaceName = string.IsNullOrWhiteSpace(runtimeStorage.TablespaceName)
            ? "pg_default"
            : runtimeStorage.TablespaceName;
        var databaseLabel = string.IsNullOrWhiteSpace(runtimeStorage.DatabaseName)
            ? config.Storage.Database
            : $"PostgreSQL ({runtimeStorage.DatabaseName}, tablespace: {tablespaceName})";

        return config with
        {
            Storage = config.Storage with
            {
                Database = databaseLabel,
                StorageUsed = runtimeStorage.StorageUsedGiB,
                StorageLimit = storageLimit
            }
        };
    }

    private async Task<RuntimeStorageMetrics?> TryGetRuntimeStorageMetricsAsync(CancellationToken cancellationToken)
    {
        if (_connection is null)
        {
            return null;
        }

        const string sql = """
                           WITH runtime_settings AS (
                             SELECT
                               current_database() AS DatabaseName,
                               COALESCE(
                                 NULLIF(current_setting('sddp.storage_tablespace', true), ''),
                                 NULLIF(current_setting('default_tablespace', true), '')
                               ) AS ConfiguredTablespace,
                               NULLIF(current_setting('sddp.storage_quota_gb', true), '') AS StorageQuotaGiBText
                           ),
                           resolved AS (
                             SELECT
                               runtime_settings.DatabaseName,
                               COALESCE(pg_tablespace.spcname, 'pg_default') AS TablespaceName,
                               runtime_settings.StorageQuotaGiBText
                             FROM runtime_settings
                             LEFT JOIN pg_tablespace
                               ON pg_tablespace.spcname = runtime_settings.ConfiguredTablespace
                           )
                           SELECT
                             DatabaseName,
                             TablespaceName,
                             pg_database_size(current_database())::bigint AS StorageUsedBytes,
                             StorageQuotaGiBText
                           FROM resolved
                           """;

        try
        {
            var row = await (_connection.QueryFirstOrDefaultAsync<RuntimeStorageMetricsRow>(
                new CommandDefinition(sql, cancellationToken: cancellationToken))).ConfigureAwait(false);
            if (row is null)
            {
                return null;
            }

            decimal storageLimitGiB = 0;
            if (!string.IsNullOrWhiteSpace(row.StorageQuotaGiBText)
                && decimal.TryParse(row.StorageQuotaGiBText, NumberStyles.Number, CultureInfo.InvariantCulture, out var quotaValue)
                && quotaValue > 0)
            {
                storageLimitGiB = Math.Round(quotaValue, 2, MidpointRounding.AwayFromZero);
            }

            return new RuntimeStorageMetrics(
                DatabaseName: row.DatabaseName,
                TablespaceName: row.TablespaceName,
                StorageUsedGiB: Math.Round(row.StorageUsedBytes / BytesPerGiB, 2, MidpointRounding.AwayFromZero),
                StorageLimitGiB: storageLimitGiB);
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Failed to query runtime tablespace/quota metrics from PostgreSQL. Falling back to configured values.");
            return null;
        }
    }

    private sealed record RuntimeStorageMetricsRow(
        string DatabaseName,
        string TablespaceName,
        long StorageUsedBytes,
        string? StorageQuotaGiBText);

    private sealed record RuntimeStorageMetrics(
        string DatabaseName,
        string TablespaceName,
        decimal StorageUsedGiB,
        decimal StorageLimitGiB);
}

/// <summary>
/// system settings get
/// </summary>
public sealed record GetSystemConfigGroupQuery(
    string ConfigGroup,
    GlobalUniqueId? TenantId,
    GlobalUniqueId? ProjectId) : IQuery<Dictionary<string, SystemConfigItemDto>>;

public sealed class GetSystemConfigGroupQueryHandler : IRequestHandler<GetSystemConfigGroupQuery, Dictionary<string, SystemConfigItemDto>>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetSystemConfigGroupQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<Dictionary<string, SystemConfigItemDto>> Handle(GetSystemConfigGroupQuery request, CancellationToken cancellationToken)
    {
        var configs = await (SystemConfigHelpers.GetAllConfigsWithFallbackAsync(
            _unitOfWork,
            request.TenantId,
            request.ProjectId,
            cancellationToken)).ConfigureAwait(false);

        var groupConfigs = configs.Where(c => c.ConfigGroup == request.ConfigGroup);
        var result = new Dictionary<string, SystemConfigItemDto>(StringComparer.OrdinalIgnoreCase);

        foreach (var config in groupConfigs)
        {
            // Keep the last value seen so project/tenant overrides win even if duplicate rows leak in.
            result[config.ConfigKey] = SystemConfigHelpers.ToItemDto(config);
        }

        return result;
    }
}

/// <summary>
/// system settings get
/// </summary>
public sealed record GetSystemConfigValueQuery(
    string ConfigGroup,
    string ConfigKey,
    GlobalUniqueId? TenantId,
    GlobalUniqueId? ProjectId) : IQuery<SystemConfigItemDto?>;

public sealed class GetSystemConfigValueQueryHandler : IRequestHandler<GetSystemConfigValueQuery, SystemConfigItemDto?>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetSystemConfigValueQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<SystemConfigItemDto?> Handle(GetSystemConfigValueQuery request, CancellationToken cancellationToken)
    {
        var configs = await (SystemConfigHelpers.GetAllConfigsWithFallbackAsync(
            _unitOfWork,
            request.TenantId,
            request.ProjectId,
            cancellationToken)).ConfigureAwait(false);

        var config = configs.FirstOrDefault(c => c.ConfigGroup == request.ConfigGroup && c.ConfigKey == request.ConfigKey);

        return config != null ? SystemConfigHelpers.ToItemDto(config) : null;
    }
}
