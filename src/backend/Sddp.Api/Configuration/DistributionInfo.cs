using System.Text.Json;

namespace Sddp.Api.Configuration;

/// <summary>Immutable build edition, distinct from runtime execution policy.</summary>
public sealed record DistributionInfo(string Edition, bool AiInstalled, bool DeploymentEnabled)
{
    public static DistributionInfo Load(IConfiguration configuration)
    {
        using var stream = typeof(DistributionInfo).Assembly.GetManifestResourceStream("Sddp.Distribution.json")
            ?? throw new InvalidOperationException("Distribution manifest missing.");
        using var reader = new StreamReader(stream);
        var installed = File.Exists(Path.Combine(AppContext.BaseDirectory, "Sddp.AI.dll"));
        return Parse(reader.ReadToEnd(), installed, configuration.GetValue<bool>("FeatureFlags:Ai:Enabled"));
    }

    public static DistributionInfo Parse(string json, bool installed, bool enabled)
    {
        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;
        if (root.ValueKind != JsonValueKind.Object || root.EnumerateObject().Count() != 2
            || root.EnumerateObject().Select(p => p.Name).Distinct().Count() != 2 || !root.TryGetProperty("schemaVersion", out var schema)
            || schema.GetInt32() != 1 || !root.TryGetProperty("edition", out var value))
            throw new InvalidOperationException("Invalid distribution manifest.");
        var edition = value.GetString();
        if (edition is not ("free" or "ai"))
            throw new InvalidOperationException("Invalid distribution edition.");
        var available = edition == "ai" && installed;
        return new(edition, available, available && enabled);
    }
}
