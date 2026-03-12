using Sddp.Abstractions.DTOs;
using Sddp.Domain.Entities;

namespace Sddp.Application.Features.Artifacts;

internal static class ArtifactMappingMapper
{
    internal static ArtifactToSpecMappingDto MapToDto(ArtifactToSpecMapping mapping)
    {
        return new ArtifactToSpecMappingDto(
            Id: mapping.Id.ToString(),
            TenantId: mapping.TenantId.ToString(),
            ProjectId: mapping.ProjectId.ToString(),
            SpecId: mapping.SpecId.ToString(),
            ArtifactPath: mapping.ArtifactPath,
            MappingReason: mapping.MappingReason.ToString(),
            Notes: mapping.Notes,
            HasSource: !string.IsNullOrEmpty(mapping.SourceContent),
            CreatedAt: mapping.CreatedAt.ToDateTimeOffset(),
            UpdatedAt: mapping.UpdatedAt.ToDateTimeOffset());
    }
}
