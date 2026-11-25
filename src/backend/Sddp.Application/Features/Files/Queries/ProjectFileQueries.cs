using MediatR;
using Sddp.Abstractions.DTOs;
using Sddp.Abstractions.Exceptions;
using Sddp.Abstractions.Interfaces;
using Sddp.Abstractions.ValueObjects;
using Sddp.Application.Requests;
using Sddp.Domain.Entities;

namespace Sddp.Application.Features.Files.Queries;

public sealed record GetProjectFileTreeQuery(GlobalUniqueId TenantId, GlobalUniqueId ProjectId, string Path, string? Revision,
    bool IncludeOwners = false, bool IncludeLastChangedBy = false) : IQuery<ProjectFileTreeDto>;
public sealed record SearchProjectFilesQuery(GlobalUniqueId TenantId, GlobalUniqueId ProjectId, string Query, string? Revision, int Skip) : IQuery<ProjectFileSearchDto>;
public sealed record GetProjectFileContentQuery(GlobalUniqueId TenantId, GlobalUniqueId ProjectId, string Path, string? Revision) : IQuery<ProjectFileContentDto>;
public sealed record GetProjectFileHistoryQuery(GlobalUniqueId TenantId, GlobalUniqueId ProjectId, string Path, string? Revision, int Skip) : IQuery<ProjectFileHistoryDto>;

public sealed class ProjectFileQueryHandlers(IProjectFileRepository files, IUnitOfWork unitOfWork) :
    IRequestHandler<GetProjectFileTreeQuery, ProjectFileTreeDto>,
    IRequestHandler<SearchProjectFilesQuery, ProjectFileSearchDto>,
    IRequestHandler<GetProjectFileContentQuery, ProjectFileContentDto>,
    IRequestHandler<GetProjectFileHistoryQuery, ProjectFileHistoryDto>
{
    public Task<ProjectFileSearchDto> Handle(SearchProjectFilesQuery request, CancellationToken cancellationToken)
        => files.SearchAsync(request.TenantId, request.ProjectId, request.Query, request.Revision, request.Skip, cancellationToken);
    public async Task<ProjectFileTreeDto> Handle(GetProjectFileTreeQuery request, CancellationToken cancellationToken)
    {
        var tree = await files.GetTreeAsync(request.TenantId, request.ProjectId, request.Path, request.Revision,
            cancellationToken, request.IncludeLastChangedBy).ConfigureAwait(false);
        if (request.IncludeLastChangedBy)
        {
            var authors = await ProjectFileAuthorProfiles.ResolveAsync(unitOfWork, request.TenantId, request.ProjectId,
                tree.Entries.Select(entry => entry.GitAuthorEmail), cancellationToken).ConfigureAwait(false);
            tree = tree with
            {
                Entries = tree.Entries.Select(entry => entry with
                {
                    LastChangedByProfile = ProjectFileAuthorProfiles.Find(authors, entry.GitAuthorEmail),
                    GitAuthorEmail = null
                }).ToList()
            };
        }
        if (!request.IncludeOwners) return tree;

        var paths = tree.Entries.Where(entry => entry.Kind != "directory").Select(entry => entry.Path).ToHashSet(StringComparer.Ordinal);
        if (paths.Count == 0) return tree;

        var project = await unitOfWork.Repository<Project>().GetByIdAsync(request.ProjectId, cancellationToken).ConfigureAwait(false);
        if (project is null || !project.IsActive || project.TenantId != request.TenantId)
            throw new NotFoundException("Project", request.ProjectId.ToString());

        var artifacts = await unitOfWork.Repository<ArtifactTracking>().FindAsync(a =>
            a.TenantId == request.TenantId && a.ProjectId == request.ProjectId && a.IsActive && a.OwnerUserId.HasValue,
            cancellationToken).ConfigureAwait(false);
        var matches = artifacts.Select(a => (Path: ProjectFileOwnership.RepositoryPath(project.ArtifactRootPath, a.ArtifactPath),
            Owner: a.OwnerUserId!.Value)).Where(a => paths.Contains(a.Path)).ToList();
        var owners = await ProjectFileOwnership.ReadOwnersAsync(unitOfWork, matches.Select(a => a.Owner), cancellationToken).ConfigureAwait(false);
        var byPath = matches.Where(a => owners.ContainsKey(a.Owner)).Distinct()
            .ToLookup(a => a.Path, a => owners[a.Owner], StringComparer.Ordinal);
        return tree with
        {
            Entries = tree.Entries.Select(entry => entry.Kind == "directory" ? entry : entry with
            {
                Owners = byPath[entry.Path].OrderBy(owner => owner.Name, StringComparer.OrdinalIgnoreCase).ThenBy(owner => owner.Id).ToList()
            }).ToList()
        };
    }
    public async Task<ProjectFileContentDto> Handle(GetProjectFileContentQuery request, CancellationToken cancellationToken)
    {
        var content = await files.GetContentAsync(request.TenantId, request.ProjectId, request.Path, request.Revision, cancellationToken).ConfigureAwait(false);
        if (content.LastChange is not { GitAuthorEmail: { } email } change) return content;
        var authors = await ProjectFileAuthorProfiles.ResolveAsync(unitOfWork, request.TenantId, request.ProjectId, [email], cancellationToken).ConfigureAwait(false);
        return content with { LastChange = WithAuthorProfile(change, authors) };
    }

    public async Task<ProjectFileHistoryDto> Handle(GetProjectFileHistoryQuery request, CancellationToken cancellationToken)
    {
        var history = await files.GetHistoryAsync(request.TenantId, request.ProjectId, request.Path, request.Revision, request.Skip, cancellationToken).ConfigureAwait(false);
        var authors = await ProjectFileAuthorProfiles.ResolveAsync(unitOfWork, request.TenantId, request.ProjectId,
            history.Items.Select(change => change.GitAuthorEmail), cancellationToken).ConfigureAwait(false);
        return history with { Items = history.Items.Select(change => WithAuthorProfile(change, authors)).ToList() };
    }

    private static ProjectFileCommitDto WithAuthorProfile(ProjectFileCommitDto change, IReadOnlyDictionary<string, UserRefDto> authors) =>
        change with { AuthorProfile = ProjectFileAuthorProfiles.Find(authors, change.GitAuthorEmail), GitAuthorEmail = null };
}
