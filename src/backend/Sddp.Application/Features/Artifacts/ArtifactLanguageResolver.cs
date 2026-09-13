namespace Sddp.Application.Features.Artifacts;

/// <summary>
/// Maps an artifact path to a syntax-highlighting language id. Resolved server-side so the
/// editor does not have to carry its own extension table.
/// </summary>
public static class ArtifactLanguageResolver
{
    private static readonly Dictionary<string, string> LanguageByExtension =
        new(StringComparer.OrdinalIgnoreCase)
        {
            [".cs"] = "csharp",
            [".ts"] = "typescript",
            [".tsx"] = "typescript",
            [".js"] = "javascript",
            [".jsx"] = "javascript",
            [".svelte"] = "html",
            [".json"] = "json",
            [".yaml"] = "yaml",
            [".yml"] = "yaml",
            [".sql"] = "sql",
            [".md"] = "markdown",
            [".css"] = "css",
            [".html"] = "html",
            [".xml"] = "xml",
            [".sh"] = "shell",
        };

    public static string FromPath(string? artifactPath)
    {
        if (string.IsNullOrWhiteSpace(artifactPath))
            return "plaintext";

        var extension = Path.GetExtension(artifactPath);
        return LanguageByExtension.GetValueOrDefault(extension, "plaintext");
    }
}
