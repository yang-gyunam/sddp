using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Caching.Distributed;
using Sddp.Abstractions.Interfaces;
using Sddp.Abstractions.ValueObjects;
using StackExchange.Redis;

namespace Sddp.Api.Services;

/// <summary>
/// Redis-based refresh token store.
/// </summary>
public class RedisRefreshTokenStore : IRefreshTokenStore
{
    private const string KeyPrefix = "auth:refresh:";
    private const string UserGenerationKeyPrefix = "auth:refresh-generation:";
    private const char StoredValueSeparator = '|';
    private readonly IDistributedCache _cache;
    private readonly IConnectionMultiplexer _redis;
    private readonly string _instanceName;

    public RedisRefreshTokenStore(
        IDistributedCache cache,
        IConnectionMultiplexer redis,
        RedisCacheKeyPrefix keyPrefix)
    {
        _cache = cache;
        _redis = redis;
        _instanceName = keyPrefix.Value;
    }

    // Match Microsoft.Extensions.Caching.StackExchangeRedis's hash format so existing sessions
    // and RedisCache readers remain compatible. Validate inputs before any script writes.
    private const string StoreScript = """
        if redis.call('EXISTS', KEYS[1]) == 1 then return 0 end
        local generation = redis.call('HGET', KEYS[2], 'data')
        if not generation then
            generation = ARGV[2]
            redis.call('HSET', KEYS[2], 'absexp', '-1', 'sldexp', '-1', 'data', generation)
        end
        redis.call('HSET', KEYS[1], 'absexp', ARGV[3], 'sldexp', '-1', 'data', ARGV[1] .. '|' .. generation)
        if ARGV[5] ~= '' then redis.call('HSET', KEYS[1], 'tenant', ARGV[5]) end
        redis.call('PEXPIRE', KEYS[1], ARGV[4])
        return 1
        """;

    private const string RotateScript = """
        local value = redis.call('HGET', KEYS[1], 'data')
        local generation = redis.call('HGET', KEYS[2], 'data')
        if not value or not generation or value ~= ARGV[1] .. '|' .. generation then return 0 end
        local tenant = redis.call('HGET', KEYS[1], 'tenant')
        if ARGV[4] ~= '' and tenant ~= ARGV[4] then return 0 end
        if redis.call('EXISTS', KEYS[3]) == 1 then return 0 end
        redis.call('HSET', KEYS[3], 'absexp', ARGV[2], 'sldexp', '-1', 'data', value)
        if tenant then redis.call('HSET', KEYS[3], 'tenant', tenant) end
        redis.call('PEXPIRE', KEYS[3], ARGV[3])
        redis.call('DEL', KEYS[1])
        return 1
        """;

    public async Task StoreAsync(GlobalUniqueId userId, string refreshToken, TimeSpan ttl, CancellationToken cancellationToken = default, GlobalUniqueId? tenantId = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(refreshToken);
        var (expiresAt, milliseconds) = Expiration(ttl);
        cancellationToken.ThrowIfCancellationRequested();
        var result = await _redis.GetDatabase().ScriptEvaluateAsync(StoreScript,
            new RedisKey[] { _instanceName + BuildKey(refreshToken), _instanceName + BuildUserGenerationKey(userId) },
            new RedisValue[] { userId.ToString(), Guid.NewGuid().ToString("N"), expiresAt, milliseconds, tenantId?.ToString() ?? string.Empty });
        if ((int)result != 1) throw new InvalidOperationException("Refresh token already exists");
    }

    public async Task<GlobalUniqueId?> GetUserIdAsync(string refreshToken, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(refreshToken))
        {
            return null;
        }

        var key = BuildKey(refreshToken);
        var value = await _cache.GetStringAsync(key, cancellationToken);

        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var separatorIndex = value.IndexOf(StoredValueSeparator, StringComparison.Ordinal);
        if (separatorIndex <= 0 || separatorIndex == value.Length - 1)
        {
            await _cache.RemoveAsync(key, cancellationToken);
            return null;
        }

        var userIdValue = value[..separatorIndex];
        var issuedGeneration = value[(separatorIndex + 1)..];

        if (!GlobalUniqueId.TryParse(userIdValue, out var userId))
        {
            return null;
        }

        var currentGeneration = await _cache.GetStringAsync(
            BuildUserGenerationKey(userId),
            cancellationToken);
        if (currentGeneration is null
            || !string.Equals(issuedGeneration, currentGeneration, StringComparison.Ordinal))
        {
            await _cache.RemoveAsync(key, cancellationToken);
            return null;
        }

        return userId;
    }

    public async Task<RefreshTokenSession?> GetSessionAsync(string refreshToken, CancellationToken cancellationToken = default)
    {
        var userId = await GetUserIdAsync(refreshToken, cancellationToken);
        if (userId is null) return null;
        var tenant = await _redis.GetDatabase().HashGetAsync(_instanceName + BuildKey(refreshToken), "tenant");
        return GlobalUniqueId.TryParse(tenant.ToString(), out var tenantId) && tenantId != GlobalUniqueId.Empty
            ? new RefreshTokenSession(userId.Value, tenantId) : null;
    }

    public async Task<GlobalUniqueId?> ConsumeAsync(
        string refreshToken,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(refreshToken))
        {
            return null;
        }

        var key = BuildKey(refreshToken);
        var value = await _cache.GetStringAsync(key, cancellationToken);

        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        // The read above is shared — several callers can see the same token. DEL is atomic and
        // reports whether this caller is the one that removed the key, so exactly one of them
        // proceeds. IDistributedCache cannot report that, hence the direct connection; it also
        // stores entries as hashes, so this has to be DEL rather than GETDEL. The cache prefixes
        // every key with the configured instance name, so we must too.
        var claimed = await _redis.GetDatabase().KeyDeleteAsync(_instanceName + key);
        if (!claimed)
        {
            return null;
        }

        var separatorIndex = value.IndexOf(StoredValueSeparator, StringComparison.Ordinal);
        if (separatorIndex <= 0 || separatorIndex == value.Length - 1)
        {
            return null;
        }

        if (!GlobalUniqueId.TryParse(value[..separatorIndex], out var userId))
        {
            return null;
        }

        // The token is gone either way now; a revoked session must still be refused.
        var issuedGeneration = value[(separatorIndex + 1)..];
        var currentGeneration = await _cache.GetStringAsync(
            BuildUserGenerationKey(userId),
            cancellationToken);

        return currentGeneration is not null
            && string.Equals(issuedGeneration, currentGeneration, StringComparison.Ordinal)
            ? userId
            : null;
    }

    public async Task<bool> RotateAsync(GlobalUniqueId expectedUserId, string refreshToken, string replacementToken,
        TimeSpan ttl, CancellationToken cancellationToken = default, GlobalUniqueId? expectedTenantId = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(refreshToken);
        ArgumentException.ThrowIfNullOrWhiteSpace(replacementToken);
        var (expiresAt, milliseconds) = Expiration(ttl);
        cancellationToken.ThrowIfCancellationRequested();
        var result = await _redis.GetDatabase().ScriptEvaluateAsync(RotateScript,
            new RedisKey[] { _instanceName + BuildKey(refreshToken),
                _instanceName + BuildUserGenerationKey(expectedUserId), _instanceName + BuildKey(replacementToken) },
            new RedisValue[] { expectedUserId.ToString(), expiresAt, milliseconds, expectedTenantId?.ToString() ?? string.Empty });
        return (int)result == 1;
    }

    public async Task RemoveAsync(string refreshToken, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(refreshToken))
        {
            return;
        }

        var key = BuildKey(refreshToken);
        await _cache.RemoveAsync(key, cancellationToken);
    }

    public Task RevokeAllAsync(
        GlobalUniqueId userId,
        CancellationToken cancellationToken = default)
    {
        var generation = Guid.NewGuid().ToString("N");
        return _cache.SetStringAsync(
            BuildUserGenerationKey(userId),
            generation,
            cancellationToken);
    }

    private static (long ExpiresAt, long Milliseconds) Expiration(TimeSpan ttl)
    {
        if (ttl <= TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(ttl));
        return (DateTimeOffset.UtcNow.Add(ttl).Ticks, checked((long)Math.Ceiling(ttl.TotalMilliseconds)));
    }

    private static string BuildKey(string refreshToken)
    {
        return $"{KeyPrefix}{ComputeHash(refreshToken)}";
    }

    private static string BuildUserGenerationKey(GlobalUniqueId userId)
    {
        return $"{UserGenerationKeyPrefix}{userId}";
    }

    private static string ComputeHash(string input)
    {
        using var sha = SHA256.Create();
        var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexString(bytes);
    }
}
