using Sddp.Abstractions.Models;
using Sddp.Domain.Entities;

namespace Sddp.Application.Features.Artifacts;

/// <summary>
/// Projects an <see cref="ArtifactTracking"/> record (and the Spec it came from) into a
/// descriptor. Every field is read from existing columns — the descriptor adds no stored
/// state of its own, which is why this feature needs no schema change.
/// </summary>
public static class ArtifactDescriptorBuilder
{
    /// <param name="spec">
    /// The Spec row referenced by <see cref="ArtifactTracking.SpecId"/>. Because Specs are
    /// versioned as separate rows, the id pins one version and re-reading it later yields
    /// the same snapshot. Null when the Spec is unavailable, which leaves the spec block
    /// off rather than emitting a hollow one.
    /// </param>
    public static ArtifactDescriptor Build(ArtifactTracking artifact, Spec? spec)
    {
        ArgumentNullException.ThrowIfNull(artifact);

        return new ArtifactDescriptor
        {
            SchemaVersion = ArtifactDescriptor.CurrentSchemaVersion,
            Artifact = new ArtifactDescriptorArtifact
            {
                Id = artifact.Id.ToString(),
                Path = artifact.ArtifactPath,
                Type = artifact.ArtifactType.ToString(),
                Category = artifact.ArtifactCategory,
                Entity = NullIfBlank(artifact.EntityName),
            },
            Generated = new ArtifactDescriptorGeneration
            {
                At = artifact.UpdatedAt.ToDateTimeOffset(),
                ContentHash = artifact.ContentHash,
                GeneratorVersion = NullIfBlank(artifact.GeneratorVersion),
                TemplateVersion = NullIfBlank(artifact.TemplateVersion),
            },
            Spec = spec is null ? null : new ArtifactDescriptorSpec
            {
                Id = spec.Id.ToString(),
                Code = spec.Code,
                Version = spec.Version.ToString(),
                Title = spec.Title,
                Context = NullIfBlank(spec.Context),
                Decision = NullIfBlank(spec.Decision),
                AcceptanceCriteria = NullIfBlank(spec.AcceptanceCriteria),
            },
            Source = new ArtifactDescriptorSource
            {
                ConversationId = artifact.SourceConversationId?.ToString(),
                RequirementId = artifact.SourceRequirementId?.ToString(),
            },
        };
    }

    private static string? NullIfBlank(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value;
}
