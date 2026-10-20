using Sddp.Abstractions.DTOs;
using Sddp.Abstractions.ValueObjects;
namespace Sddp.Abstractions.Interfaces;

public interface IGitConnectionService
{
    Task<GitConnectionListDto> ListAsync(GlobalUniqueId tenant, GlobalUniqueId? project, CancellationToken ct);
    Task<GitConnectionDto> CreateAsync(GlobalUniqueId tenant, CreateGitConnectionDto input, CancellationToken ct);
    Task<GitConnectionDto> UpdateAsync(GlobalUniqueId tenant, GlobalUniqueId id, UpdateGitConnectionDto input, CancellationToken ct);
    Task DeleteAsync(GlobalUniqueId tenant, GlobalUniqueId id, int version, CancellationToken ct);
}
public interface IGitCredentialProtector
{
    string Protect(string token, string purpose);
    string Unprotect(string ciphertext, string purpose);
}
public sealed record GitEditionPolicy(bool AllowsPrivateRepositories);
public sealed record GitRemoteAccess(string Url, bool IsPrivate, string? Username = null, string? Token = null)
{
    public override string ToString() => "GitRemoteAccess { credentials redacted }";
}
public interface IGitRepositoryAccess
{
    Task<GitRemoteAccess> ResolveAsync(GlobalUniqueId tenant, GlobalUniqueId project, string url,
        GlobalUniqueId? connectionId, CancellationToken ct);
    Task<GitRemoteAccess> ResolveForReadAsync(GlobalUniqueId tenant, GlobalUniqueId project, string url,
        GlobalUniqueId? connectionId, CancellationToken ct) =>
        ResolveAsync(tenant, project, url, connectionId, ct);
}
