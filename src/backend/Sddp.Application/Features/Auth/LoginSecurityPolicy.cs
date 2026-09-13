namespace Sddp.Application.Features.Auth;

/// <summary>
/// Account-level protection applied after the endpoint-level IP limiter.
/// </summary>
public sealed record LoginSecurityPolicy(int MaxFailedAttempts, TimeSpan LockoutDuration)
{
    public const int DefaultMaxFailedAttempts = 5;
    public const int DefaultLockoutMinutes = 15;

    public static LoginSecurityPolicy Default { get; } = new(
        DefaultMaxFailedAttempts,
        TimeSpan.FromMinutes(DefaultLockoutMinutes));
}
