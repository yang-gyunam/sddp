using Sddp.Abstractions.DTOs;
using Sddp.Abstractions.Exceptions;
using Sddp.Abstractions.Interfaces;
using Sddp.Abstractions.ValueObjects;
using Sddp.Domain.Entities;
namespace Sddp.Infrastructure.Services;

public sealed class GitConnectionService(IUnitOfWork unit, IGitCredentialProtector protector,
    IGitRemoteInspector inspector, IRepositoryConnectionChecker checker, GitEditionPolicy edition) : IGitConnectionService
{
    public async Task<GitConnectionListDto> ListAsync(GlobalUniqueId tenant, GlobalUniqueId? project, CancellationToken ct)
    {
        var rows = await unit.Repository<GitConnection>().FindAsync(x => x.TenantId == tenant && x.IsActive && (project == null || x.ProjectId == project), ct);
        return new(edition.AllowsPrivateRepositories, rows.Select(Map).OrderBy(x => x.Name).ToList());
    }
    public async Task<GitConnectionDto> CreateAsync(GlobalUniqueId tenant, CreateGitConnectionDto input, CancellationToken ct)
    {
        if (!GlobalUniqueId.TryParse(input.ProjectId, out var projectId)) throw new SddpException("INVALID_ID", "Select an allowed project.");
        await GetProject(tenant, projectId, ct);
        Validate(input.Name, input.Username, input.Token);
        var origin = inspector.ValidateServer(input.Provider, input.ServerUrl.Trim());
        var connection = new GitConnection(tenant, projectId, input.Name.Trim(), input.Provider, origin);
        string? cipher = null;
        if (!string.IsNullOrEmpty(input.Token))
        {
            GitRepositoryAccess.RequirePrivateEdition(edition);
            await inspector.CheckCredentialAsync(input.Provider, origin, input.Username, input.Token, ct);
            cipher = protector.Protect(input.Token, GitRepositoryAccess.Purpose(connection));
        }
        connection.Update(input.Name.Trim(), input.Username.Trim(), cipher, cipher is null ? null : DateTimeOffset.UtcNow);
        await unit.Repository<GitConnection>().AddAsync(connection, ct);
        await unit.SaveChangesAsync(ct);
        return Map(connection);
    }
    public async Task<GitConnectionDto> UpdateAsync(GlobalUniqueId tenant, GlobalUniqueId id, UpdateGitConnectionDto input, CancellationToken ct)
    {
        var connection = await Get(tenant, id, ct);
        connection.CheckVersion(input.ExpectedVersion);
        Validate(input.Name, input.Username, input.Token);
        if (input.RemoveToken && input.Token is not null) throw new SddpException("VALIDATION_FAILED", "Choose either replacement or removal.");
        var project = await GetProject(tenant, connection.ProjectId, ct);
        var replacing = input.Token is not null || input.RemoveToken || input.Username != connection.Username;
        var cipher = connection.ProtectedToken;
        var checkedAt = connection.CheckedAt;
        if (replacing)
        {
            var token = input.RemoveToken ? null : input.Token ?? (cipher is null ? null : protector.Unprotect(cipher, GitRepositoryAccess.Purpose(connection)));
            if (!string.IsNullOrEmpty(token))
            {
                GitRepositoryAccess.RequirePrivateEdition(edition);
                await inspector.CheckCredentialAsync(connection.Provider, connection.ServerUrl, input.Username, token, ct);
            }
            if (project.GitConnectionId == id && project.RepoUrl.Length > 0)
            {
                var access = new GitRemoteAccess(project.RepoUrl, false, input.Username, token);
                var isPrivate = await inspector.InspectAsync(connection.Provider, connection.ServerUrl, access, ct);
                if (isPrivate) GitRepositoryAccess.RequirePrivateEdition(edition);
                await checker.CheckAsync(access with { IsPrivate = isPrivate }, project.RepoBranch, project.ArtifactRootPath, [], ct);
            }
            cipher = string.IsNullOrEmpty(token) ? null : protector.Protect(token, GitRepositoryAccess.Purpose(connection));
            checkedAt = DateTimeOffset.UtcNow;
            if (project.GitConnectionId == id) project.InvalidateRepositoryAccess();
        }
        connection.Update(input.Name.Trim(), input.Username.Trim(), cipher, checkedAt);
        await unit.SaveChangesAsync(ct);
        return Map(connection);
    }
    public async Task DeleteAsync(GlobalUniqueId tenant, GlobalUniqueId id, int version, CancellationToken ct)
    {
        var connection = await Get(tenant, id, ct); connection.CheckVersion(version);
        var project = await GetProject(tenant, connection.ProjectId, ct);
        if (project.GitConnectionId == id) throw new ConflictException("Select another project connection before removing this connection.");
        connection.Deactivate();
        await unit.SaveChangesAsync(ct);
    }
    private async Task<GitConnection> Get(GlobalUniqueId tenant, GlobalUniqueId id, CancellationToken ct)
    {
        var row = await unit.Repository<GitConnection>().GetByIdAsync(id, ct);
        if (row is null || row.TenantId != tenant || !row.IsActive) throw new NotFoundException("Git connection", id.ToString());
        return row;
    }
    private async Task<Project> GetProject(GlobalUniqueId tenant, GlobalUniqueId id, CancellationToken ct)
    {
        var row = await unit.Repository<Project>().GetByIdAsync(id, ct);
        if (row is null || row.TenantId != tenant || !row.IsActive) throw new NotFoundException("Project", id.ToString());
        return row;
    }
    private static void Validate(string name, string username, string? token)
    {
        if (string.IsNullOrWhiteSpace(name) || name.Length > 100 || username.Length > 200 || username.Contains(':')
            || username.Any(char.IsControl) || token?.Length > 4096 || token?.Any(char.IsControl) == true || token == "")
            throw new SddpException("VALIDATION_FAILED", "Enter a connection name and valid Git credentials. Leave token unset to preserve it.");
    }
    private static GitConnectionDto Map(GitConnection x) => new(x.Id, x.ProjectId, x.Name, x.Provider, x.ServerUrl, x.Username, x.ProtectedToken is not null, x.Version, x.CheckedAt);
}
