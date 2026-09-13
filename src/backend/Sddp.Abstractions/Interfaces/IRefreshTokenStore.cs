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
    Task StoreAsync(GlobalUniqueId userId, string refreshToken, TimeSpan ttl, CancellationToken cancellationToken = default, GlobalUniqueId? tenantId = null);

    /// <summary>
    /// Refresh Token user ID get
    /// </summary>
    Task<GlobalUniqueId?> GetUserIdAsync(string refreshToken, CancellationToken cancellationToken = default);

    /// <summary>Returns only sessions bound to a tenant. Legacy unbound tokens require login.</summary>
    Task<RefreshTokenSession?> GetSessionAsync(string refreshToken, CancellationToken cancellationToken = default);

    /// <summary>
    /// Claims a token for one caller. This is not a consume-and-replace transaction;
    /// refresh handlers must use RotateAsync instead.
    ///
    /// Refresh tokens are single-use. Reading and then deleting leaves a window where two
    /// concurrent refreshes both see the token as valid and both mint a replacement, so
    /// rotation has to happen in one step rather than as a read followed by a delete.
    /// </summary>
    Task<GlobalUniqueId?> ConsumeAsync(string refreshToken, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates the expected user and current generation, consumes the old token and stores its
    /// replacement atomically. False means no replacement was issued. Never compose Consume/Store.
    /// </summary>
    Task<bool> RotateAsync(GlobalUniqueId expectedUserId, string refreshToken, string replacementToken,
        TimeSpan ttl, CancellationToken cancellationToken = default, GlobalUniqueId? expectedTenantId = null);

    /// <summary>
    /// Refresh Token
    /// </summary>
    Task RemoveAsync(string refreshToken, CancellationToken cancellationToken = default);

    /// <summary>
    /// Invalidates every refresh token issued for the user before this call.
    /// </summary>
    Task RevokeAllAsync(GlobalUniqueId userId, CancellationToken cancellationToken = default);
}

public sealed record RefreshTokenSession(GlobalUniqueId UserId, GlobalUniqueId TenantId);
