using System.Security.Claims;
using Sddp.Abstractions.ValueObjects;

namespace Sddp.Abstractions.Interfaces;

/// <summary>
/// JWT
/// </summary>
public interface IJwtService
{
    /// <summary>
    /// Access Token create
    /// </summary>
    string GenerateAccessToken(GlobalUniqueId userId, string username, GlobalUniqueId tenantId, IEnumerable<string> roles, IEnumerable<string> permissions, string? displayName = null);

    /// <summary>
    /// Refresh Token create
    /// </summary>
    string GenerateRefreshToken();

    /// <summary>
    /// ClaimsPrincipal
    /// </summary>
    ClaimsPrincipal? GetPrincipalFromExpiredToken(string token);

    /// <summary>
    /// Access Token ()
    /// </summary>
    int AccessTokenExpirationMinutes { get; }

    /// <summary>
    /// Refresh Token ()
    /// </summary>
    int RefreshTokenExpirationDays { get; }
}
