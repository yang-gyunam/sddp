namespace Sddp.Api.Constants;

public static class AuthCookieNames
{
    public const string HubAccessToken = "sddp-hub-access-token";
    public const string RefreshToken = "__Host-sddp-refresh-token";
    public const string CsrfToken = "__Host-sddp-csrf";
    public const string CsrfHeader = "X-CSRF-Token";
}
