using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Caching.Distributed;
using Sddp.Abstractions.Interfaces;
using Sddp.Abstractions.ValueObjects;

namespace Sddp.Api.Services;

/// <summary>
/// Redis-based refresh token store.
/// </summary>
public class RedisRefreshTokenStore : IRefreshTokenStore
{
    private const string KeyPrefix = "auth:refresh:";
    private readonly IDistributedCache _cache;

    public RedisRefreshTokenStore(IDistributedCache cache)
    {
        _cache = cache;
    }

    public async Task StoreAsync(GlobalUniqueId userId, string refreshToken, TimeSpan ttl, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(refreshToken))
        {
            throw new ArgumentException("Refresh token cannot be empty", nameof(refreshToken));
        }

        var key = BuildKey(refreshToken);
        var options = new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = ttl
        };

        await _cache.SetStringAsync(key, userId.ToString(), options, cancellationToken);
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

        return GlobalUniqueId.TryParse(value, out var userId) ? userId : null;
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

    private static string BuildKey(string refreshToken)
    {
        return $"{KeyPrefix}{ComputeHash(refreshToken)}";
    }

    private static string ComputeHash(string input)
    {
        using var sha = SHA256.Create();
        var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexString(bytes);
    }
}
