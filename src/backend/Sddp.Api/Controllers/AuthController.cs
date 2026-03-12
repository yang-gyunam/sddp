using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sddp.Abstractions.DTOs;
using Sddp.Abstractions.Exceptions;
using Sddp.Abstractions.ValueObjects;
using Sddp.Api.Constants;
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

    public AuthController(ISender sender, ILogger<AuthController> logger)
    {
        _sender = sender;
        _logger = logger;
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken cancellationToken)
    {
        var response = await _sender.Send(new LoginCommand(request), cancellationToken);
        SetHubAccessTokenCookie(response.AccessToken, response.ExpiresIn);
        return Ok(ApiResponse<LoginResponse>.Ok(response));
    }

    [HttpPost("refresh")]
    [AllowAnonymous]
    public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequest? request, CancellationToken cancellationToken)
    {
        var refreshToken = ResolveRefreshToken(request);
        if (string.IsNullOrWhiteSpace(refreshToken))
        {
            throw new UnauthorizedException("Refresh token is required");
        }

        var response = await _sender.Send(new RefreshTokenCommand(refreshToken), cancellationToken);
        SetHubAccessTokenCookie(response.AccessToken, response.ExpiresIn);
        return Ok(ApiResponse<TokenResponse>.Ok(response));
    }

    /// <summary>
    /// change (log user)
    /// POST /api/auth/change-password
    /// </summary>
    [HttpPost("change-password")]
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
    [Authorize]
    public async Task<IActionResult> Logout([FromBody] RefreshTokenRequest? request, CancellationToken cancellationToken)
    {
        var userId = HttpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        var refreshToken = ResolveRefreshToken(request);
        await _sender.Send(new LogoutCommand(refreshToken), cancellationToken);
        ClearHubAccessTokenCookie();

        _logger.LogInformation("User logged out: {UserId}", userId);

        return NoContent();
    }

    private string? ResolveRefreshToken(RefreshTokenRequest? request)
    {
        return string.IsNullOrWhiteSpace(request?.RefreshToken)
            ? null
            : request.RefreshToken;
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
