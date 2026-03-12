using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Sddp.Abstractions.Interfaces;
using Sddp.Abstractions.ValueObjects;

namespace Sddp.Api.Services;

/// <summary>
/// JWT token service implementation.
/// </summary>
public class JwtService : IJwtService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<JwtService> _logger;
    private readonly string _secret;
    private readonly string _issuer;
    private readonly string _audience;

    public int AccessTokenExpirationMinutes { get; }
    public int RefreshTokenExpirationDays { get; }

    public JwtService(
        IConfiguration configuration,
        ILogger<JwtService> logger)
    {
        _configuration = configuration;
        _logger = logger;

        var jwtSection = configuration.GetSection("Jwt");
        _secret = jwtSection["Secret"] ?? throw new InvalidOperationException("JWT Secret is not configured");
        _issuer = jwtSection["Issuer"] ?? "sddp-api";
        _audience = jwtSection["Audience"] ?? "sddp-client";
        AccessTokenExpirationMinutes = int.TryParse(jwtSection["AccessTokenExpirationMinutes"], out var accessMinutes)
            ? accessMinutes : 30;
        RefreshTokenExpirationDays = int.TryParse(jwtSection["RefreshTokenExpirationDays"], out var refreshDays)
            ? refreshDays : 7;
    }

    public string GenerateAccessToken(
        GlobalUniqueId userId,
        string username,
        GlobalUniqueId tenantId,
        IEnumerable<string> roles,
        IEnumerable<string> permissions,
        string? displayName = null)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_secret));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, userId.ToString()),
            new(ClaimTypes.NameIdentifier, userId.ToString()),
            new(JwtRegisteredClaimNames.UniqueName, username),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(JwtRegisteredClaimNames.Iat, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64),
            new("tenant_id", tenantId.ToString()),
            new("name", displayName ?? username)
        };

        // Add role claims.
        foreach (var role in roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        // Add permission claims.
        foreach (var permission in permissions)
        {
            claims.Add(new Claim("permission", permission));
        }

        var token = new JwtSecurityToken(
            issuer: _issuer,
            audience: _audience,
            claims: claims,
            notBefore: DateTime.UtcNow,
            expires: DateTime.UtcNow.AddMinutes(AccessTokenExpirationMinutes),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public string GenerateRefreshToken()
    {
        var randomBytes = new byte[64];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomBytes);
        return Convert.ToBase64String(randomBytes);
    }

    public ClaimsPrincipal? GetPrincipalFromExpiredToken(string token)
    {
        var tokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = false, // Allows validation of expired tokens.
            ValidateIssuerSigningKey = true,
            ValidIssuer = _issuer,
            ValidAudience = _audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_secret))
        };

        try
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var principal = tokenHandler.ValidateToken(token, tokenValidationParameters, out var securityToken);

            if (securityToken is not JwtSecurityToken jwtSecurityToken ||
                !jwtSecurityToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase))
            {
                return null;
            }

            return principal;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "Failed to parse expired JWT token. tokenLength={TokenLength}, exceptionType={ExceptionType}",
                token?.Length ?? 0,
                ex.GetType().Name);
            return null;
        }
    }
}
