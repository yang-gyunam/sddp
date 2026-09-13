namespace Sddp.Api.Services;

/// <summary>
/// The prefix AddStackExchangeRedisCache puts in front of every key it writes.
///
/// Anything reaching Redis directly rather than through IDistributedCache has to apply the
/// same prefix, or it silently addresses a different key than the cache wrote.
/// </summary>
/// <param name="Value">Configured Redis:InstanceName, for example "sddp:".</param>
public sealed record RedisCacheKeyPrefix(string Value);
