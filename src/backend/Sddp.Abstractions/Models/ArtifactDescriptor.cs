namespace Sddp.Abstractions.Models;

public sealed record ArtifactDescriptor
{
    /// <summary>
    /// Format version of the descriptor itself, so older files stay readable when the
    /// shape changes. Distinct from <see cref="ArtifactDescriptorSpec.Version"/>, which
    /// tracks the Spec the artifact was generated from.
    /// </summary>
    public int SchemaVersion { get; init; } = CurrentSchemaVersion;

    public const int CurrentSchemaVersion = 1;

    public ArtifactDescriptorArtifact Artifact { get; init; } = new();
    public ArtifactDescriptorGeneration Generated { get; init; } = new();

    /// <summary>
    /// Snapshot of the Spec as it was when the artifact was generated. Carried in full so
    /// an agent holding only this file can judge the artifact without querying SDDP.
    /// </summary>
    public ArtifactDescriptorSpec? Spec { get; init; }

    public ArtifactDescriptorSource Source { get; init; } = new();
}

public sealed record ArtifactDescriptorArtifact
{
    public string Id { get; init; } = string.Empty;

    /// <summary>Path relative to the project's artifact root.</summary>
    public string Path { get; init; } = string.Empty;

    public string Type { get; init; } = string.Empty;
    public string Category { get; init; } = string.Empty;
    public string? Entity { get; init; }
}

public sealed record ArtifactDescriptorGeneration
{
    public DateTimeOffset At { get; init; }

    /// <summary>
    /// Hash of the file as generated. Comparing this against the file on disk is what
    /// tells a reader whether the artifact still matches what SDDP produced.
    /// </summary>
    public string ContentHash { get; init; } = string.Empty;

    public string? GeneratorVersion { get; init; }
    public string? TemplateVersion { get; init; }
}

public sealed record ArtifactDescriptorSpec
{
    public string Id { get; init; } = string.Empty;
    public string Code { get; init; } = string.Empty;
    public string Version { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public string? Context { get; init; }
    public string? Decision { get; init; }
    public string? AcceptanceCriteria { get; init; }
}

public sealed record ArtifactDescriptorSource
{
    public string? ConversationId { get; init; }
    public string? RequirementId { get; init; }
}
