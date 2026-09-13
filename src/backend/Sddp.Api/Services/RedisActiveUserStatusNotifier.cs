using Microsoft.Extensions.Caching.Distributed;
using Sddp.Abstractions.Interfaces;
using Sddp.Abstractions.Security;
using Sddp.Abstractions.ValueObjects;
using StackExchange.Redis;

namespace Sddp.Api.Services;

/// <summary>
/// Stores the race-free distributed refusal and broadcasts cache/connection invalidation to every
/// API process through Redis pub/sub.
/// </summary>
public sealed class RedisActiveUserStatusNotifier : IActiveUserStatusNotifier, IHostedService
{
    private static readonly byte[] DeactivatedMarker = [1];

    private readonly IDistributedCache _distributedCache;
    private readonly ActiveUserStatusLocalSink _localSink;
    private readonly ISubscriber _subscriber;
    private readonly RedisChannel _channel;
    private readonly ILogger<RedisActiveUserStatusNotifier> _logger;

    public RedisActiveUserStatusNotifier(
        IDistributedCache distributedCache,
        ActiveUserStatusLocalSink localSink,
        IConnectionMultiplexer redis,
        RedisCacheKeyPrefix keyPrefix,
        ILogger<RedisActiveUserStatusNotifier> logger)
    {
        _distributedCache = distributedCache;
        _localSink = localSink;
        _subscriber = redis.GetSubscriber();
        _channel = RedisChannel.Literal($"{keyPrefix.Value}active-user-status");
        _logger = logger;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return _subscriber.SubscribeAsync(_channel, OnMessage);
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return _subscriber.UnsubscribeAsync(_channel, OnMessage);
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

        // Apply before publishing so the command host is closed even if Redis reports no remote
        // subscribers. The marker still protects every host's next REST/hub validation.
        var abortedConnections = _localSink.Apply(userId, isActive);
        var payload = $"{(isActive ? 'A' : 'D')}|{userId}";
        var subscribers = await _subscriber.PublishAsync(_channel, payload).ConfigureAwait(false);

        if (subscribers == 0)
        {
            throw new InvalidOperationException(
                "No API host is subscribed to active-user status events.");
        }

        _logger.LogInformation(
            "Published active-user status for {UserId}: active={IsActive}, subscribers={Subscribers}, localAborts={LocalAborts}",
            userId,
            isActive,
            subscribers,
            abortedConnections);
    }

    private void OnMessage(RedisChannel _, RedisValue value)
    {
        var payload = value.ToString();
        var separator = payload.IndexOf('|');
        if (separator != 1
            || payload[0] is not ('A' or 'D')
            || !GlobalUniqueId.TryParse(payload[(separator + 1)..], out var userId))
        {
            _logger.LogWarning("Ignored malformed active-user status event");
            return;
        }

        var isActive = payload[0] == 'A';
        var abortedConnections = _localSink.Apply(userId, isActive);
        _logger.LogDebug(
            "Applied active-user status for {UserId}: active={IsActive}, aborted={AbortedConnections}",
            userId,
            isActive,
            abortedConnections);
    }
}
