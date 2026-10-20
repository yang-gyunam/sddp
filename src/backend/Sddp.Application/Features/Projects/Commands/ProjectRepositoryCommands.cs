using MediatR;
using Sddp.Abstractions.DTOs;
using Sddp.Abstractions.Enums;
using Sddp.Abstractions.Exceptions;
using Sddp.Abstractions.Interfaces;
using Sddp.Abstractions.ValueObjects;
using Sddp.Application.Requests;
using Sddp.Domain.Entities;

namespace Sddp.Application.Features.Projects.Commands;

public sealed record GetProjectRepositoryQuery(GlobalUniqueId TenantId, GlobalUniqueId ProjectId)
    : IQuery<ProjectRepositoryDto>;
public sealed record SaveProjectRepositoryCommand(GlobalUniqueId TenantId, GlobalUniqueId ProjectId,
    GlobalUniqueId UserId, UpdateProjectRepositoryDto Settings) : ICommand<ProjectRepositoryDto>;
public sealed record CheckProjectRepositoryCommand(GlobalUniqueId TenantId, GlobalUniqueId ProjectId,
    int ExpectedVersion) : ICommand<ProjectRepositoryDto>;
public sealed record ConfirmProjectRepositoryCommand(GlobalUniqueId TenantId, GlobalUniqueId ProjectId,
    GlobalUniqueId UserId, int ExpectedVersion) : ICommand<ProjectRepositoryDto>, IAuditableRequest<ProjectRepositoryDto>
{
    public AuditLogRequest? GetAuditLogRequest(ProjectRepositoryDto response) => new(UserId,
        "confirm_repository", "project", ProjectId,
        new { response.RepoUrl, response.RepoBranch, response.ArtifactRootPath, response.ConfirmedAt }, TenantId, ProjectId);
}

public sealed class ProjectRepositoryCommandHandler(IUnitOfWork unitOfWork, IRepositoryConnectionChecker checker, IGitRepositoryAccess access)
    : IRequestHandler<GetProjectRepositoryQuery, ProjectRepositoryDto>,
      IRequestHandler<SaveProjectRepositoryCommand, ProjectRepositoryDto>,
      IRequestHandler<CheckProjectRepositoryCommand, ProjectRepositoryDto>,
      IRequestHandler<ConfirmProjectRepositoryCommand, ProjectRepositoryDto>
{
    public async Task<ProjectRepositoryDto> Handle(GetProjectRepositoryQuery request, CancellationToken ct)
    {
        var project = await GetProject(request.TenantId, request.ProjectId, ct);
        return Map(project, (await GetPaths(project, ct)).Count);
    }

    public async Task<ProjectRepositoryDto> Handle(SaveProjectRepositoryCommand request, CancellationToken ct)
    {
        var project = await GetProject(request.TenantId, request.ProjectId, ct);
        project.CheckRepositoryVersion(request.Settings.ExpectedVersion);
        var settings = request.Settings;
        var url = settings.RepoUrl.Trim();
        var branch = settings.RepoBranch.Trim();
        var root = settings.ArtifactRootPath.Trim().TrimEnd('/');
        GlobalUniqueId? connectionId = null;
        if (settings.GitConnectionId is not null)
        {
            if (!GlobalUniqueId.TryParse(settings.GitConnectionId, out var parsed) || parsed.IsEmpty)
                throw new SddpException("VALIDATION_FAILED", "Select a valid Git connection.");
            connectionId = parsed;
        }
        Validate(url, branch, root, connectionId is not null);
        if (settings.SyncIntervalMinutes is < 1 or > 10080)
            throw new SddpException("VALIDATION_FAILED", "Sync interval must be between 1 and 10080 minutes.");
        var paths = await GetPaths(project, ct);
        if (paths.Count > 0 && ((project.RepoUrl.Length > 0 && project.RepoUrl != url)
            || project.RepoBranch != branch || project.ArtifactRootPath != root))
            throw new ConflictException("Existing file relationships depend on this repository configuration. Review a repository migration before changing it.");
        var remote = await access.ResolveAsync(request.TenantId, request.ProjectId, url, connectionId, ct);
        if (project.GitConnectionId != connectionId && (project.RepositoryConfirmedAt.HasValue || paths.Count > 0))
            await checker.CheckAsync(remote, branch, root, paths, ct);
        project.UpdateRepository(url, branch, root, settings.SyncIntervalMinutes);
        project.SelectGitConnection(connectionId);
        project.RecordRepositoryVisibility(remote.IsPrivate);
        await unitOfWork.SaveChangesAsync(ct);
        return Map(project, paths.Count);
    }

    public async Task<ProjectRepositoryDto> Handle(CheckProjectRepositoryCommand request, CancellationToken ct)
    {
        var project = await GetProject(request.TenantId, request.ProjectId, ct);
        project.CheckRepositoryVersion(request.ExpectedVersion);
        EnsureWritable(project);
        Validate(project.RepoUrl, project.RepoBranch, project.ArtifactRootPath, project.GitConnectionId is not null);
        var paths = await GetPaths(project, ct);
        var remote = await access.ResolveAsync(request.TenantId, request.ProjectId, project.RepoUrl, project.GitConnectionId, ct);
        await checker.CheckAsync(remote, project.RepoBranch, project.ArtifactRootPath, paths, ct);
        project.RecordRepositoryVisibility(remote.IsPrivate);
        project.RecordRepositoryCheck(DateTimeOffset.UtcNow);
        await unitOfWork.SaveChangesAsync(ct);
        return Map(project, paths.Count);
    }

    public async Task<ProjectRepositoryDto> Handle(ConfirmProjectRepositoryCommand request, CancellationToken ct)
    {
        var project = await GetProject(request.TenantId, request.ProjectId, ct);
        project.CheckRepositoryVersion(request.ExpectedVersion);
        EnsureWritable(project);
        var remote = await access.ResolveAsync(request.TenantId, request.ProjectId, project.RepoUrl, project.GitConnectionId, ct);
        project.RecordRepositoryVisibility(remote.IsPrivate);
        project.ConfirmRepository(DateTimeOffset.UtcNow);
        await unitOfWork.SaveChangesAsync(ct);
        return Map(project, (await GetPaths(project, ct)).Count);
    }

    private async Task<Project> GetProject(GlobalUniqueId tenantId, GlobalUniqueId id, CancellationToken ct)
    {
        var project = await unitOfWork.Repository<Project>().GetByIdAsync(id, ct);
        if (project is null || project.TenantId != tenantId || !project.IsActive)
            throw new NotFoundException("Project", id.ToString());
        return project;
    }

    private async Task<List<string>> GetPaths(Project project, CancellationToken ct)
    {
        var artifacts = await unitOfWork.Repository<ArtifactTracking>().FindAsync(
            a => a.TenantId == project.TenantId && a.ProjectId == project.Id && a.IsActive, ct);
        var mappings = await unitOfWork.Repository<ArtifactToSpecMapping>().FindAsync(
            a => a.TenantId == project.TenantId && a.ProjectId == project.Id && a.IsActive, ct);
        var prefix = project.ArtifactRootPath.Trim('/');
        return artifacts.Select(a => prefix.Length == 0 ? a.ArtifactPath : $"{prefix}/{a.ArtifactPath}")
            .Concat(mappings.Select(m => m.ArtifactPath)).Distinct(StringComparer.Ordinal).ToList();
    }

    private static void EnsureWritable(Project project)
    {
        if (project.Status.IsReadOnly()) throw new ConflictException("This project is read-only.");
    }

    private static ProjectRepositoryDto Map(Project project, int count) => new(project.RepoUrl,
        project.RepoBranch, project.ArtifactRootPath, project.SyncIntervalMinutes, project.RepositoryVersion,
        project.RepositoryCheckedAt, project.RepositoryConfirmedAt, count,
        !project.RepositoryConfirmedAt.HasValue && project.RepositoryCheckedAt >= DateTimeOffset.UtcNow.AddMinutes(-15),
        project.Status.IsReadOnly(), project.GitConnectionId?.ToString(), project.RepositoryVisibility);

    public static void Validate(string url, string branch, string root, bool allowHttp = false)
    {
        if (url.Length > 2048 || !Uri.TryCreate(url, UriKind.Absolute, out var uri)
            || (uri.Scheme != Uri.UriSchemeHttps && !(allowHttp && uri.Scheme == Uri.UriSchemeHttp)) || string.IsNullOrEmpty(uri.Host)
            || uri.UserInfo.Length > 0 || uri.Query.Length > 0 || uri.Fragment.Length > 0
            || url.Any(char.IsControl))
            throw new SddpException("VALIDATION_FAILED", "Use an HTTPS repository URL without credentials, query parameters or fragments.");
        if (branch.Length is < 1 or > 255 || branch.StartsWith('-') || branch.StartsWith('/')
            || branch.EndsWith('/') || branch.EndsWith('.') || branch.Contains("..") || branch.Contains("@{")
            || branch.Contains("//") || branch == "@" || branch.Any(c => char.IsWhiteSpace(c) || char.IsControl(c) || "~^:?*[\\".Contains(c))
            || branch.Split('/').Any(p => p.StartsWith('.') || p.EndsWith(".lock", StringComparison.Ordinal)))
            throw new SddpException("VALIDATION_FAILED", "Enter a valid Git branch name.");
        if (root.Length > 1024 || root.Contains('\\') || root.Any(char.IsControl)
            || (root.Length > 0 && root.Split('/').Any(p => p is "" or "." or "..")))
            throw new SddpException("VALIDATION_FAILED", "Artifact root must be a repository-relative directory, without dot segments.");
    }
}
