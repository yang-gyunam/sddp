namespace Sddp.Abstractions.DTOs;

/// <summary>
/// Authentication-related DTOs.
/// </summary>
public class LoginRequest
{
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public record LoginResponse(string AccessToken, string RefreshToken, int ExpiresIn, UserInfo User, bool RequirePasswordChange);

public class RefreshTokenRequest
{
    public string? RefreshToken { get; set; }
}

public record TokenResponse(
    string AccessToken,
    string RefreshToken,
    int ExpiresIn,
    UserInfo? User = null,
    bool RequirePasswordChange = false);

public record UserInfo(string Id, string Username, string DisplayName, string TenantId, IEnumerable<string> Roles, IEnumerable<string> Permissions);

public class ChangePasswordRequest
{
    public string CurrentPassword { get; set; } = string.Empty;
    public string NewPassword { get; set; } = string.Empty;
}

public record AdminResetPasswordResponse(string TemporaryPassword);
