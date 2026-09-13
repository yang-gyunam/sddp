using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Sddp.Abstractions.DTOs;
using Sddp.Abstractions.Exceptions;
using Sddp.Abstractions.ValueObjects;
using Sddp.Api.Authorization;
using Sddp.Api.Constants;
using Sddp.Api.Security;
using Sddp.Application.Features.Auth.Commands;

namespace Sddp.Api.Controllers;

/// <summary>
/// controller
/// </summary>
[Route("api/auth")]
public class AuthController : BaseApiController
{
    private readonly ISender _sender;
    private readonly ILogger<AuthController> _logger;
    private readonly IConfiguration _configuration;

    public AuthController(
        ISender sender,
        ILogger<AuthController> logger,
        IConfiguration configuration)
    {
        _sender = sender;
        _logger = logger;
        _configuration = configuration;
    }

    [HttpPost("login")]
    [ResourceScopeExempt("Anonymous login endpoint: no session, tenant or project exists yet, and the login rate limiter is what bounds abuse.")]
    [AllowAnonymous]
    [EnableRateLimiting(AuthenticationSecurity.LoginPolicyName)]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken cancellationToken)
    {
        LoginResponse response;
        try
        {
            response = await _sender.Send(new LoginCommand(request), cancellationToken);
        }
        catch (TenantSelectionRequiredException selection)
        {
            return Conflict(new ApiResponse<object>
            {
                Success = false, ErrorCode = selection.ErrorCode, ErrorMessage = selection.Message,
                Data = new { selection.TenantIds }
            });
        }
        SetRefreshSessionCookies(response.RefreshToken);
        SetHubAccessTokenCookie(response.AccessToken, response.ExpiresIn);
        return Ok(ApiResponse<LoginResponse>.Ok(response));
    }

    [HttpPost("refresh")]
    [ResourceScopeExempt("Anonymous refresh keyed by the caller's own refresh cookie and a matching CSRF header; no project-owned resource is addressed.")]
    [AllowAnonymous]
    public async Task<IActionResult> RefreshToken(CancellationToken cancellationToken)
    {
        if (!HasValidCsrfToken())
        {
            return CsrfValidationFailed();
        }

        var refreshToken = ResolveRefreshToken();
        if (string.IsNullOrWhiteSpace(refreshToken))
        {
            throw new UnauthorizedException("Refresh token is required");
        }

        var response = await _sender.Send(new RefreshTokenCommand(refreshToken), cancellationToken);
        SetRefreshSessionCookies(response.RefreshToken);
        SetHubAccessTokenCookie(response.AccessToken, response.ExpiresIn);
        return Ok(ApiResponse<TokenResponse>.Ok(response));
    }

    /// <summary>
    /// change (log user)
    /// POST /api/auth/change-password
    /// </summary>
    [HttpPost("change-password")]
    [ResourceScopeExempt("Keyed by the caller's own NameIdentifier claim, so a user can only ever change their own password; no project takes part.")]
    [Authorize]
    [MutationAuthorizationExempt("An authenticated user may change only their own password.")]
    [Authorize]
    public async Task<IActionResult> ChangePassword(
        [FromBody] ChangePasswordRequest request,
        CancellationToken cancellationToken)
    {
        var userIdClaim = HttpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !GlobalUniqueId.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized();
        }

        await _sender.Send(new ChangePasswordCommand(userId, request), cancellationToken);
        return Ok(ApiResponse<object>.Ok(new { changed = true }));
    }

    [HttpPost("logout")]
    [ResourceScopeExempt("Anonymous logout keyed by the caller's own refresh cookie and a matching CSRF header; no project-owned resource is addressed.")]
    [AllowAnonymous]
    public async Task<IActionResult> Logout(CancellationToken cancellationToken)
    {
        if (!HasValidCsrfToken())
        {
            return CsrfValidationFailed();
        }

        var refreshToken = ResolveRefreshToken();
        await _sender.Send(new LogoutCommand(refreshToken), cancellationToken);
        ClearRefreshSessionCookies();
        ClearHubAccessTokenCookie();

        _logger.LogInformation("Refresh-token session logged out");

        return NoContent();
    }

    private string? ResolveRefreshToken()
    {
        var refreshToken = Request.Cookies[AuthCookieNames.RefreshToken];
        return string.IsNullOrWhiteSpace(refreshToken) ? null : refreshToken;
    }

    private bool HasValidCsrfToken()
    {
        var cookieToken = Request.Cookies[AuthCookieNames.CsrfToken];
        var headerToken = Request.Headers[AuthCookieNames.CsrfHeader].ToString();
        if (string.IsNullOrWhiteSpace(cookieToken)
            || string.IsNullOrWhiteSpace(headerToken)
            || cookieToken.Length != headerToken.Length)
        {
            return false;
        }

        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(cookieToken),
            Encoding.UTF8.GetBytes(headerToken));
    }

    private ObjectResult CsrfValidationFailed()
    {
        return Problem(
            statusCode: StatusCodes.Status403Forbidden,
            title: "CSRF validation failed",
            detail: "A matching CSRF cookie and X-CSRF-Token header are required.");
    }

    private void SetRefreshSessionCookies(string refreshToken)
    {
        var maxAge = TimeSpan.FromDays(Math.Max(
            1,
            _configuration.GetValue("Jwt:RefreshTokenExpirationDays", 7)));
        var csrfToken = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));

        Response.Cookies.Append(
            AuthCookieNames.RefreshToken,
            refreshToken,
            CreateRefreshCookieOptions(httpOnly: true, maxAge: maxAge));
        Response.Cookies.Append(
            AuthCookieNames.CsrfToken,
            csrfToken,
            CreateRefreshCookieOptions(httpOnly: false, maxAge: maxAge));
    }

    private void ClearRefreshSessionCookies()
    {
        Response.Cookies.Delete(
            AuthCookieNames.RefreshToken,
            CreateRefreshCookieOptions(httpOnly: true));
        Response.Cookies.Delete(
            AuthCookieNames.CsrfToken,
            CreateRefreshCookieOptions(httpOnly: false));
    }

    private static CookieOptions CreateRefreshCookieOptions(
        bool httpOnly,
        TimeSpan? maxAge = null)
    {
        return new CookieOptions
        {
            HttpOnly = httpOnly,
            Secure = true,
            SameSite = SameSiteMode.Strict,
            Path = "/",
            MaxAge = maxAge,
            IsEssential = true
        };
    }

    private void SetHubAccessTokenCookie(string accessToken, int expiresInSeconds)
    {
        var isHttps = IsHttpsRequest();
        var maxAge = TimeSpan.FromSeconds(Math.Max(0, expiresInSeconds));

        Response.Cookies.Append(
            AuthCookieNames.HubAccessToken,
            accessToken,
            new CookieOptions
            {
                HttpOnly = true,
                Secure = isHttps,
                SameSite = isHttps ? SameSiteMode.None : SameSiteMode.Lax,
                Path = "/hubs",
                MaxAge = maxAge,
                IsEssential = true
            });
    }

    private void ClearHubAccessTokenCookie()
    {
        Response.Cookies.Delete(
            AuthCookieNames.HubAccessToken,
            new CookieOptions
            {
                Path = "/hubs",
                IsEssential = true
            });
    }

    private bool IsHttpsRequest()
    {
        if (Request.IsHttps)
        {
            return true;
        }

        var forwardedProto = Request.Headers["X-Forwarded-Proto"].ToString();
        return string.Equals(forwardedProto, "https", StringComparison.OrdinalIgnoreCase);
    }
}
