namespace Sddp.Abstractions.Interfaces;
public interface IGitRemoteInspector
{
    string ValidateServer(string provider, string serverUrl);
    Task<bool> InspectAsync(string provider, string serverUrl, GitRemoteAccess access, CancellationToken ct);
    Task CheckAnonymousReadAsync(string serverUrl, string repositoryUrl, CancellationToken ct);
    Task CheckCredentialAsync(string provider, string serverUrl, string username, string token, CancellationToken ct);
}
