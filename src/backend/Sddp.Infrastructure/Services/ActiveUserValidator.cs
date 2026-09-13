using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Sddp.Abstractions.Interfaces;
using Sddp.Abstractions.Security;
using Sddp.Abstractions.ValueObjects;
using Sddp.Infrastructure.Persistence.Contexts;
using Microsoft.Extensions.Logging;

namespace Sddp.Infrastructure.Services;

public sealed class ActiveUserValidator : IActiveUserGate
{
    /// <summary>
    /// Short enough that a stale entry cannot outlive a deactivation by much, long enough that a
    /// burst of requests from one caller does not become a burst of queries. Deactivation clears
    /// the entry outright, so this bounds only a host that did not handle that deactivation itself.
    /// </summary>
    internal static readonly TimeSpan CacheDuration = ActiveUserStatusKeys.LocalCacheDuration;

    private readonly SddpDbContext _context;
    private readonly IMemoryCache _cache;
    private readonly IDistributedCache _distributedCache;
    private readonly ILogger<ActiveUserValidator> _logger;

    public ActiveUserValidator(
        SddpDbContext context,
        IMemoryCache cache,
        IDistributedCache distributedCache,
        ILogger<ActiveUserValidator> logger)
    {
        _context = context;
        _cache = cache;
        _distributedCache = distributedCache;
        _logger = logger;
    }

    /// <summary>
    /// What the shared marker said, including the case where it could not be asked.
    /// </summary>
    private enum SharedMarker
    {
        Refused,
        NotRefused,
        Unavailable,
    }

    internal static string CacheKey(GlobalUniqueId userId) => ActiveUserStatusKeys.Local(userId);

    /// <summary>
    /// True when the account may still act. A user that is not there is not active either — an id
    /// in a signed token that no longer resolves is a token to refuse, not one to trust.
    /// </summary>
    public async Task<bool> IsActiveAsync(GlobalUniqueId userId, CancellationToken cancellationToken = default)
    {
        var marker = await ReadSharedMarkerAsync(userId, cancellationToken).ConfigureAwait(false);

        if (marker == SharedMarker.Refused)
        {
            _cache.Set(CacheKey(userId), false, CacheDuration);
            return false;
        }

        // The local answer is only good enough when the marker was actually read, because
        // correcting a stale local "active" is the only thing the marker is for. When the shared
        // store could not be asked, fall through to the record, which is more authoritative than
        // either cache — not to a cached guess, and not to a failed request.
        if (marker == SharedMarker.NotRefused && _cache.TryGetValue<bool>(CacheKey(userId), out var cached))
        {
            return cached;
        }

        var isActive = await _context.Users
            .AsNoTracking()
            .Where(user => user.Id == userId)
            .Select(user => (bool?)user.IsActive)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false) ?? false;

        _cache.Set(CacheKey(userId), isActive, CacheDuration);
        return isActive;
    }

    /// <summary>
    /// Reads the shared refusal marker, reporting an unreachable store rather than throwing.
    ///
    /// This runs from the JWT validation hook, so an exception here fails every authenticated
    /// request — a Redis outage would take the whole API down rather than costing token refresh,
    /// which is what it cost before the marker existed. The database is the durable authority for
    /// IsActive and is still consulted below, so a lost marker narrows the guarantee to the
    /// propagation window it was added to close; it does not open the gate.
    ///
    /// Cancellation is not a store failure and is left to propagate: it means the caller went
    /// away, and the request is over either way.
    /// </summary>
    private async Task<SharedMarker> ReadSharedMarkerAsync(
        GlobalUniqueId userId,
        CancellationToken cancellationToken)
    {
        try
        {
            var refusal = await _distributedCache.GetAsync(
                ActiveUserStatusKeys.DistributedDeactivation(userId),
                cancellationToken).ConfigureAwait(false);

            return refusal is not null ? SharedMarker.Refused : SharedMarker.NotRefused;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            _logger.LogWarning(
                exception,
                "Shared account-status marker is unreachable; falling back to the database for {UserId}. "
                + "A deactivation committed on another host within the last {CacheSeconds}s may not be "
                + "visible to this one until its cache expires.",
                userId,
                CacheDuration.TotalSeconds);

            return SharedMarker.Unavailable;
        }
    }

    public void MarkDeactivated(GlobalUniqueId userId) => _cache.Set(CacheKey(userId), false, CacheDuration);

    public void Forget(GlobalUniqueId userId) => _cache.Remove(CacheKey(userId));
}
