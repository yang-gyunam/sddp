using Sddp.Abstractions.ValueObjects;

namespace Sddp.Abstractions.Interfaces;

/// <summary>
/// Refresh Token
/// </summary>
public interface IRefreshTokenStore
{
    /// <summary>
    /// Refresh Token
    /// </summary>
    Task StoreAsync(GlobalUniqueId userId, string refreshToken, TimeSpan ttl, CancellationToken cancellationToken = default);

    /// <summary>
    /// Refresh Token user ID get
    /// </summary>
    Task<GlobalUniqueId?> GetUserIdAsync(string refreshToken, CancellationToken cancellationToken = default);

    /// <summary>
    /// Refresh Token
    /// </summary>
    Task RemoveAsync(string refreshToken, CancellationToken cancellationToken = default);
}
