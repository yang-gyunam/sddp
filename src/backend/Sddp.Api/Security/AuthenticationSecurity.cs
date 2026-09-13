using System.Globalization;
using System.Net;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
using Sddp.Abstractions.DTOs;
using Sddp.Application.Features.Auth;
using System.Security.Claims;
using Sddp.Api.Constants;

namespace Sddp.Api.Security;

public static class AuthenticationSecurity
{
    public const string LoginPolicyName = "Login";

    public static IServiceCollection AddAuthenticationSecurity(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var maxFailedAttempts = ReadBounded(
            configuration,
            "Authentication:LoginProtection:MaxFailedAttempts",
            LoginSecurityPolicy.DefaultMaxFailedAttempts,
            minimum: 1,
            maximum: 100);
        var lockoutMinutes = ReadBounded(
            configuration,
            "Authentication:LoginProtection:LockoutMinutes",
            LoginSecurityPolicy.DefaultLockoutMinutes,
            minimum: 1,
            maximum: 1440);
        var permitLimit = ReadBounded(
            configuration,
            "Authentication:LoginProtection:IpPermitLimit",
            defaultValue: 10,
            minimum: 1,
            maximum: 1000);
        var windowSeconds = ReadBounded(
            configuration,
            "Authentication:LoginProtection:IpWindowSeconds",
            defaultValue: 60,
            minimum: 1,
            maximum: 3600);
        var trustForwardedFor = configuration.GetValue(
            "Authentication:LoginProtection:TrustForwardedFor",
            false);

        var clientErrorPermitLimit = ReadBounded(
            configuration,
            "ErrorReporter:ClientReportsPerMinute",
            defaultValue: 30,
            minimum: 1,
            maximum: 10000);

        services.AddSingleton(new LoginSecurityPolicy(
            maxFailedAttempts,
            TimeSpan.FromMinutes(lockoutMinutes)));

        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.OnRejected = WriteRateLimitResponseAsync;
            options.AddPolicy(
                LoginPolicyName,
                context => RateLimitPartition.GetFixedWindowLimiter(
                    ResolveClientPartition(context, trustForwardedFor),
                    _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = permitLimit,
                        Window = TimeSpan.FromSeconds(windowSeconds),
                        QueueLimit = 0,
                        AutoReplenishment = true
                    }));

            options.AddPolicy(
                RateLimitPolicies.ClientErrorReporting,
                context => RateLimitPartition.GetFixedWindowLimiter(
                    ResolveReporterPartition(context, trustForwardedFor),
                    _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = clientErrorPermitLimit,
                        Window = TimeSpan.FromSeconds(60),
                        QueueLimit = 0,
                        AutoReplenishment = true
                    }));
        });

        return services;
    }

    /// <summary>
    /// Partitions the error relay by whoever is calling. The user id is preferred because it is
    /// the thing being bounded; the address is the fallback for a request that has not been
    /// authenticated yet, which the limiter sees before the endpoint refuses it.
    /// </summary>
    private static string ResolveReporterPartition(HttpContext context, bool trustForwardedFor)
    {
        var subject = context.User?.FindFirst("sub")?.Value
            ?? context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        return string.IsNullOrWhiteSpace(subject)
            ? $"ip:{ResolveClientPartition(context, trustForwardedFor)}"
            : $"user:{subject}";
    }

    private static string ResolveClientPartition(HttpContext context, bool trustForwardedFor)
    {
        if (trustForwardedFor)
        {
            var forwardedFor = context.Request.Headers["X-Forwarded-For"].ToString();
            var candidate = forwardedFor.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .FirstOrDefault();
            if (IPAddress.TryParse(candidate, out var forwardedAddress))
            {
                return forwardedAddress.ToString();
            }
        }

        return context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    }

    private static async ValueTask WriteRateLimitResponseAsync(
        OnRejectedContext context,
        CancellationToken cancellationToken)
    {
        var retryAfterSeconds = 60;
        if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
        {
            retryAfterSeconds = Math.Max(1, (int)Math.Ceiling(retryAfter.TotalSeconds));
        }

        context.HttpContext.Response.Headers.RetryAfter =
            retryAfterSeconds.ToString(CultureInfo.InvariantCulture);

        await context.HttpContext.Response.WriteAsJsonAsync(
            ApiResponse<object>.Fail(
                "LOGIN_RATE_LIMITED",
                $"Too many sign-in attempts. Try again in {retryAfterSeconds} seconds."),
            cancellationToken);
    }

    private static int ReadBounded(
        IConfiguration configuration,
        string key,
        int defaultValue,
        int minimum,
        int maximum)
    {
        var value = configuration.GetValue(key, defaultValue);
        if (value < minimum || value > maximum)
        {
            throw new InvalidOperationException(
                $"Configuration '{key}' must be between {minimum} and {maximum}.");
        }

        return value;
    }
}
