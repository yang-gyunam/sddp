using Microsoft.Extensions.Caching.Memory;
using Sddp.Abstractions.Interfaces;
using Sddp.Abstractions.Security;
using Sddp.Abstractions.ValueObjects;

namespace Sddp.Api.Services;

/// <summary>
/// Applies an account status to one API process after a local commit or distributed event.
/// </summary>
public sealed class ActiveUserStatusLocalSink
{
    private readonly IMemoryCache _cache;
    private readonly IUserConnectionRegistry _connections;

    public ActiveUserStatusLocalSink(IMemoryCache cache, IUserConnectionRegistry connections)
    {
        _cache = cache;
        _connections = connections;
    }

    public int Apply(GlobalUniqueId userId, bool isActive)
    {
        if (isActive)
        {
            _cache.Remove(ActiveUserStatusKeys.Local(userId));
            return 0;
        }

        _cache.Set(
            ActiveUserStatusKeys.Local(userId),
            false,
            ActiveUserStatusKeys.LocalCacheDuration);
        return _connections.AbortAll(userId);
    }
}
