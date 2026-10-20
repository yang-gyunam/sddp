using Sddp.Abstractions.Exceptions;
using Sddp.Abstractions.Interfaces;
using Sddp.Abstractions.ValueObjects;
using Sddp.Domain.Entities;
namespace Sddp.Infrastructure.Services;

public sealed class GitRepositoryAccess(IUnitOfWork unit, IGitCredentialProtector protector,
    IGitRemoteInspector inspector, GitEditionPolicy edition) : IGitRepositoryAccess
{
    public Task<GitRemoteAccess> ResolveAsync(GlobalUniqueId tenant, GlobalUniqueId project, string url,
        GlobalUniqueId? connectionId, CancellationToken ct) =>
        ResolveAsync(tenant, project, url, connectionId, false, ct);

    public Task<GitRemoteAccess> ResolveForReadAsync(GlobalUniqueId tenant, GlobalUniqueId project, string url,
        GlobalUniqueId? connectionId, CancellationToken ct) =>
        ResolveAsync(tenant, project, url, connectionId, true, ct);

    private async Task<GitRemoteAccess> ResolveAsync(GlobalUniqueId tenant, GlobalUniqueId project, string url,
        GlobalUniqueId? connectionId, bool forRead, CancellationToken ct)
    {
        var provider = "GitHub";
        var server = "https://github.com";
        string? token = null, username = null;
        if (connectionId is { } id)
        {
            var connection = await unit.Repository<GitConnection>().GetByIdAsync(id, ct);
            if (connection is null || !connection.IsActive || connection.TenantId != tenant || connection.ProjectId != project)
                throw new SddpException("GIT_CONNECTION_UNAVAILABLE", "The selected connection is unavailable for this project.");
            provider = connection.Provider; server = connection.ServerUrl; username = connection.Username;
            if (connection.ProtectedToken is not null)
            {
                // A Free deployment never sends a previously saved private credential.
                RequirePrivateEdition(edition);
                token = protector.Unprotect(connection.ProtectedToken, Purpose(connection));
            }
        }
        var access = new GitRemoteAccess(url, false, username, token);
        if (forRead && provider == "GitHub" && string.IsNullOrEmpty(token))
        {
            // Successful anonymous Git discovery proves current public readability without
            // spending the GitHub REST quota or retaining an access grant after revocation.
            await inspector.CheckAnonymousReadAsync(server, url, ct);
            return access;
        }
        var isPrivate = await inspector.InspectAsync(provider, server, access, ct);
        if (isPrivate) RequirePrivateEdition(edition);
        return access with { IsPrivate = isPrivate };
    }
    public static string Purpose(GitConnection connection) => $"{connection.TenantId}/{connection.ProjectId}/{connection.Id}";
    public static void RequirePrivateEdition(GitEditionPolicy edition)
    { if (!edition.AllowsPrivateRepositories) throw new SddpException("PRIVATE_REPOSITORY_NOT_AVAILABLE", "Free edition supports public repositories only."); }
}
