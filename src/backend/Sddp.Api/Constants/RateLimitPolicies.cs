namespace Sddp.Api.Constants;

/// <summary>
/// Names of the rate limit policies this service registers, so a controller and the registration
/// cannot drift apart over a string literal.
/// </summary>
public static class RateLimitPolicies
{
    public const string ClientErrorReporting = "ClientErrorReporting";
}
