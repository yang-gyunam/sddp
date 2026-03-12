using MediatR;
using Sddp.Abstractions.DTOs;
using Sddp.Abstractions.Interfaces.Snapshots;
using Sddp.Abstractions.ValueObjects;
using Sddp.Application.Requests;

namespace Sddp.Application.Features.ProjectSnapshots.Queries;

/// <summary>
/// project get
/// </summary>
public sealed record GetProjectSnapshotsQuery(
    GlobalUniqueId TenantId,
    GlobalUniqueId ProjectId) : IQuery<IReadOnlyList<ProjectSnapshotDto>>;

public sealed class GetProjectSnapshotsQueryHandler
    : IRequestHandler<GetProjectSnapshotsQuery, IReadOnlyList<ProjectSnapshotDto>>
{
    private readonly IProjectSnapshotService _snapshotService;

    public GetProjectSnapshotsQueryHandler(IProjectSnapshotService snapshotService)
    {
        _snapshotService = snapshotService;
    }

    public async Task<IReadOnlyList<ProjectSnapshotDto>> Handle(
        GetProjectSnapshotsQuery request, CancellationToken cancellationToken)
    {
        return await (_snapshotService.GetSnapshotsAsync(
            request.TenantId,
            request.ProjectId,
            cancellationToken)).ConfigureAwait(false);
    }
}
