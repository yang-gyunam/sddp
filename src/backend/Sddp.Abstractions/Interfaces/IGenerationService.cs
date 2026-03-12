using Sddp.Abstractions.ValueObjects;

namespace Sddp.Abstractions.Interfaces;

/// <summary>
/// create
/// </summary>
public interface IGenerationService
{
    /// <summary>
    /// Spec create
    /// </summary>
    Task<GenerationResult> GenerateFromSpecAsync(GlobalUniqueId specId, GenerationOptions options, CancellationToken cancellationToken = default);

    /// <summary>
    /// create Artifact get
    /// </summary>
    Task<IReadOnlyList<GeneratedArtifact>> GetArtifactsBySpecIdAsync(GlobalUniqueId specId, CancellationToken cancellationToken = default);

    /// <summary>
    /// create
    /// </summary>
    Task<bool> RollbackAsync(GlobalUniqueId generationId, CancellationToken cancellationToken = default);

    /// <summary>
    /// create (create)
    /// </summary>
    Task<GenerationPreview> PreviewAsync(GlobalUniqueId specId, GenerationOptions options, CancellationToken cancellationToken = default);
}

/// <summary>
/// create
/// </summary>
public class GenerationOptions
{
    public bool GenerateEntity { get; set; } = true;
    public bool GenerateDto { get; set; } = true;
    public bool GenerateRepository { get; set; } = true;
    public bool GenerateService { get; set; } = true;
    public bool GenerateController { get; set; } = true;
    public string? OutputDirectory { get; set; }
}

/// <summary>
/// create
/// </summary>
public class GenerationResult
{
    public GlobalUniqueId GenerationId { get; init; }
    public GlobalUniqueId SpecId { get; init; }
    public bool Success { get; init; }
    public IReadOnlyList<GeneratedArtifact> Artifacts { get; init; } = [];
    public IReadOnlyList<string> Errors { get; init; } = [];
    public Timestamp GeneratedAt { get; init; }
}

/// <summary>
/// create Artifact
/// </summary>
public class GeneratedArtifact
{
    public GlobalUniqueId ArtifactId { get; init; }
    public GlobalUniqueId SpecId { get; init; }
    public string ArtifactType { get; init; } = string.Empty;
    public string FilePath { get; init; } = string.Empty;
    public string Content { get; init; } = string.Empty;
    public Timestamp GeneratedAt { get; init; }
}

/// <summary>
/// create
/// </summary>
public class GenerationPreview
{
    public IReadOnlyList<GeneratedArtifact> PreviewArtifacts { get; init; } = [];
    public IReadOnlyList<string> Warnings { get; init; } = [];
}
