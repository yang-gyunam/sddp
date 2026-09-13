using System.Text;

namespace Sddp.Api.Security;

public static class ProductionSecretGuard
{
    /// <summary>
    /// Minimum length for an HMAC-SHA256 signing key. Shorter keys are padded internally, which
    /// makes a short secret look like it works while carrying far less entropy than the algorithm
    /// assumes.
    /// </summary>
    internal const int MinimumSecretLength = 32;

    /// <summary>
    /// Values that appear in this repository. Matching is case-insensitive and substring-based,
    /// because the point is to catch a secret that was never replaced — not to enumerate every
    /// literal.
    ///
    /// These are fragments rather than whole credentials on purpose. Spelling an internal
    /// credential out in full would carry it into the public build, where it is neither useful nor
    /// something to publish; "dev_password" catches the development value here and any other
    /// development password an operator forgets to swap.
    /// </summary>
    private static readonly string[] PublishedSecretMarkers =
    [
        "CHANGE_THIS",
        "change_me",
        "dev_password",
        "sddp_password",
        "your-secret",
        "changeme",
    ];

    /// <summary>
    /// Throws when the configuration would start a non-Development host on a published or weak
    /// secret. Development and Testing are exempt: the first is the profile these values exist for,
    /// the second runs against an isolated in-memory database.
    /// </summary>
    public static void EnsureSafe(IHostEnvironment environment, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(environment);
        ArgumentNullException.ThrowIfNull(configuration);

        if (environment.IsDevelopment() || environment.IsEnvironment("Testing"))
        {
            return;
        }

        var problems = Inspect(configuration);
        if (problems.Count == 0)
        {
            return;
        }

        var message = new StringBuilder()
            .AppendLine($"Refusing to start in the '{environment.EnvironmentName}' environment on secrets that are not safe:")
            .AppendLine();

        foreach (var problem in problems)
        {
            message.Append("  - ").AppendLine(problem);
        }

        message
            .AppendLine()
            .AppendLine("Provide them through environment variables or a secret manager, for example:")
            .AppendLine("  Jwt__Secret=\"$(openssl rand -base64 48)\"")
            .AppendLine("Set unique secrets for this installation using environment variables or a secret manager.");

        throw new InvalidOperationException(message.ToString());
    }

    /// <summary>
    /// Every problem, rather than the first: an operator fixing one at a time would otherwise need
    /// as many restarts as there are misconfigured values.
    /// </summary>
    internal static IReadOnlyList<string> Inspect(IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        var problems = new List<string>();
        var secret = configuration.GetSection("Jwt")["Secret"];

        if (string.IsNullOrWhiteSpace(secret))
        {
            problems.Add("Jwt:Secret is not configured.");
        }
        else
        {
            if (ContainsPublishedMarker(secret))
            {
                problems.Add(
                    "Jwt:Secret is a placeholder from this repository, so anyone reading the source can forge a token.");
            }

            if (secret.Trim().Length < MinimumSecretLength)
            {
                problems.Add(
                    $"Jwt:Secret is shorter than {MinimumSecretLength} characters, which is below what HMAC-SHA256 assumes.");
            }
        }

        var connection = configuration.GetConnectionString("DefaultConnection");
        if (!string.IsNullOrWhiteSpace(connection) && ContainsPublishedMarker(connection))
        {
            problems.Add(
                "ConnectionStrings:DefaultConnection carries a development password that is published in this repository.");
        }

        return problems;
    }

    private static bool ContainsPublishedMarker(string value) =>
        PublishedSecretMarkers.Any(marker => value.Contains(marker, StringComparison.OrdinalIgnoreCase));
}
