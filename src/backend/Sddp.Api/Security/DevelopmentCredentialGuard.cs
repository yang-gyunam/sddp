using Microsoft.EntityFrameworkCore;
using Sddp.Infrastructure.Persistence.Contexts;

namespace Sddp.Api.Security;

public static class DevelopmentCredentialGuard
{
    private const string KnownAdminUsername = "admin";
    private const string KnownAdminPassword = "Admin@123!";
    private const string LoopbackExposureMode = "Loopback";

    public static async Task EnsureSafeAsync(WebApplication app, CancellationToken cancellationToken = default)
    {
        if (app.Environment.IsEnvironment("Testing"))
        {
            return;
        }

        using var scope = app.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<SddpDbContext>();
        var adminPasswordHash = await context.Users
            .AsNoTracking()
            .Where(user => user.Username == KnownAdminUsername)
            .Select(user => user.PasswordHash)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

        EnsureKnownCredentialAllowed(app.Environment, app.Configuration, adminPasswordHash);

        if (!string.IsNullOrWhiteSpace(adminPasswordHash)
            && BCrypt.Net.BCrypt.Verify(KnownAdminPassword, adminPasswordHash))
        {
            app.Logger.LogWarning(
                "Known development administrator credentials are enabled for the loopback-only evaluation profile");
        }
    }

    internal static void EnsureKnownCredentialAllowed(
        IHostEnvironment environment,
        IConfiguration configuration,
        string? passwordHash)
    {
        if (string.IsNullOrWhiteSpace(passwordHash)
            || !BCrypt.Net.BCrypt.Verify(KnownAdminPassword, passwordHash))
        {
            return;
        }

        if (!IsKnownCredentialAllowed(environment, configuration))
        {
            throw new InvalidOperationException(
                "Known development administrator credentials are present outside the approved loopback-only " +
                "Development profile. Reset the admin password before starting the API.");
        }
    }

    internal static bool IsKnownCredentialAllowed(
        IHostEnvironment environment,
        IConfiguration configuration)
    {
        var allowKnownCredentials = configuration.GetValue(
            "Authentication:DevelopmentCredentials:AllowKnownCredentials",
            false);
        var exposureMode = configuration[
            "Authentication:DevelopmentCredentials:ExposureMode"];

        return environment.IsDevelopment()
            && allowKnownCredentials
            && string.Equals(
                exposureMode,
                LoopbackExposureMode,
                StringComparison.OrdinalIgnoreCase);
    }
}
