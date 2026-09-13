using Microsoft.Extensions.Caching.Distributed;
using Sddp.Abstractions.Interfaces;
using Sddp.Abstractions.Security;
using Sddp.Abstractions.ValueObjects;

namespace Sddp.Api.Services;

/// <summary>
/// Single-host implementation used by the explicit Testing environment.
/// </summary>
public sealed class LocalActiveUserStatusNotifier : IActiveUserStatusNotifier
{
    private static readonly byte[] DeactivatedMarker = [1];

    private readonly IDistributedCache _distributedCache;
    private readonly ActiveUserStatusLocalSink _localSink;

    public LocalActiveUserStatusNotifier(
        IDistributedCache distributedCache,
        ActiveUserStatusLocalSink localSink)
    {
        _distributedCache = distributedCache;
        _localSink = localSink;
    }

    public async Task PublishAsync(
        GlobalUniqueId userId,
        bool isActive,
        CancellationToken cancellationToken = default)
    {
        var key = ActiveUserStatusKeys.DistributedDeactivation(userId);
        if (isActive)
        {
            await _distributedCache.RemoveAsync(key, cancellationToken).ConfigureAwait(false);
        }
        else
        {
            await _distributedCache.SetAsync(
                key,
                DeactivatedMarker,
                new DistributedCacheEntryOptions(),
                cancellationToken).ConfigureAwait(false);
        }

        _localSink.Apply(userId, isActive);
    }
}
