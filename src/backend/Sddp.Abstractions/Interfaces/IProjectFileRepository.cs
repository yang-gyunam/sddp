using Sddp.Abstractions.DTOs;
using Sddp.Abstractions.ValueObjects;

namespace Sddp.Abstractions.Interfaces;

/// <summary>Bounded, committed-only access to a project's configured Git branch.</summary>
public interface IProjectFileRepository
{
    Task<ProjectFileTreeDto> GetTreeAsync(GlobalUniqueId tenantId, GlobalUniqueId projectId, string path, string? revision, CancellationToken ct);
    Task<ProjectFileSearchDto> SearchAsync(GlobalUniqueId tenantId, GlobalUniqueId projectId, string query, string? revision, int skip, CancellationToken ct);
    Task<ProjectFileContentDto> GetContentAsync(GlobalUniqueId tenantId, GlobalUniqueId projectId, string path, string? revision, CancellationToken ct);
    Task<ProjectFileHistoryDto> GetHistoryAsync(GlobalUniqueId tenantId, GlobalUniqueId projectId, string path, string? revision, int skip, CancellationToken ct);
}
