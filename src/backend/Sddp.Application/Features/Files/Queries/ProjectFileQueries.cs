using MediatR;
using Sddp.Abstractions.DTOs;
using Sddp.Abstractions.Interfaces;
using Sddp.Abstractions.ValueObjects;
using Sddp.Application.Requests;

namespace Sddp.Application.Features.Files.Queries;

public sealed record GetProjectFileTreeQuery(GlobalUniqueId TenantId, GlobalUniqueId ProjectId, string Path, string? Revision) : IQuery<ProjectFileTreeDto>;
public sealed record SearchProjectFilesQuery(GlobalUniqueId TenantId, GlobalUniqueId ProjectId, string Query, string? Revision, int Skip) : IQuery<ProjectFileSearchDto>;
public sealed record GetProjectFileContentQuery(GlobalUniqueId TenantId, GlobalUniqueId ProjectId, string Path, string? Revision) : IQuery<ProjectFileContentDto>;
public sealed record GetProjectFileHistoryQuery(GlobalUniqueId TenantId, GlobalUniqueId ProjectId, string Path, string? Revision, int Skip) : IQuery<ProjectFileHistoryDto>;

public sealed class ProjectFileQueryHandlers(IProjectFileRepository files) :
    IRequestHandler<GetProjectFileTreeQuery, ProjectFileTreeDto>,
    IRequestHandler<SearchProjectFilesQuery, ProjectFileSearchDto>,
    IRequestHandler<GetProjectFileContentQuery, ProjectFileContentDto>,
    IRequestHandler<GetProjectFileHistoryQuery, ProjectFileHistoryDto>
{
    public Task<ProjectFileSearchDto> Handle(SearchProjectFilesQuery request, CancellationToken cancellationToken)
        => files.SearchAsync(request.TenantId, request.ProjectId, request.Query, request.Revision, request.Skip, cancellationToken);
    public Task<ProjectFileTreeDto> Handle(GetProjectFileTreeQuery request, CancellationToken cancellationToken)
        => files.GetTreeAsync(request.TenantId, request.ProjectId, request.Path, request.Revision, cancellationToken);
    public Task<ProjectFileContentDto> Handle(GetProjectFileContentQuery request, CancellationToken cancellationToken)
        => files.GetContentAsync(request.TenantId, request.ProjectId, request.Path, request.Revision, cancellationToken);
    public Task<ProjectFileHistoryDto> Handle(GetProjectFileHistoryQuery request, CancellationToken cancellationToken)
        => files.GetHistoryAsync(request.TenantId, request.ProjectId, request.Path, request.Revision, request.Skip, cancellationToken);
}
