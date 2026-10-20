using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using Sddp.Abstractions.Exceptions;
using Sddp.Abstractions.Interfaces;
namespace Sddp.Infrastructure.Services;

public sealed class GitRemoteInspector(HttpClient client, IOptions<GitRepositoryOptions> options) : IGitRemoteInspector
{
    public string ValidateServer(string provider, string serverUrl)
    {
        if (provider is not ("GitHub" or "Gitea") || !Uri.TryCreate(serverUrl, UriKind.Absolute, out var uri)
            || uri.UserInfo.Length > 0 || uri.Query.Length > 0 || uri.Fragment.Length > 0 || uri.AbsolutePath != "/"
            || (uri.Scheme != "https" && !(provider == "Gitea" && uri.Scheme == "http"
                && Uri.TryCreate(options.Value.ServerUrl, UriKind.Absolute, out var internalUri)
                && internalUri.GetLeftPart(UriPartial.Authority) == uri.GetLeftPart(UriPartial.Authority))))
            throw new SddpException("INVALID_GIT_SERVER", "Use an HTTPS server origin or the configured internal Gitea server.");
        if (provider == "GitHub" && uri.GetLeftPart(UriPartial.Authority) != "https://github.com")
            throw new SddpException("INVALID_GIT_SERVER", "This GitHub connection supports github.com.");
        return uri.GetLeftPart(UriPartial.Authority);
    }

    public async Task<bool> InspectAsync(string provider, string serverUrl, GitRemoteAccess access, CancellationToken ct)
    {
        var (origin, path) = ValidateRepository(provider, serverUrl, access.Url);
        using var data = await Get(provider, origin, $"repos/{path}", access.Token, ct);
        if (data.RootElement.ValueKind != JsonValueKind.Object || !data.RootElement.TryGetProperty("private", out var visibility) || visibility.ValueKind is not (JsonValueKind.True or JsonValueKind.False))
            throw new SddpException("REPOSITORY_VISIBILITY_UNKNOWN", "The Git server did not provide repository visibility.");
        return visibility.GetBoolean();
    }

    public async Task CheckAnonymousReadAsync(string serverUrl, string repositoryUrl, CancellationToken ct)
    {
        var (origin, path) = ValidateRepository("GitHub", serverUrl, repositoryUrl);
        using var request = new HttpRequestMessage(HttpMethod.Get,
            $"{origin}/{path}.git/info/refs?service=git-upload-pack");
        request.Headers.UserAgent.ParseAdd("SDDP/0.5.0");
        request.Headers.Accept.ParseAdd("application/x-git-upload-pack-advertisement");
        request.Headers.CacheControl = new CacheControlHeaderValue { NoCache = true, NoStore = true };
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
        timeout.CancelAfter(TimeSpan.FromSeconds(15));
        try
        {
            using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, timeout.Token);
            if (response.StatusCode != System.Net.HttpStatusCode.OK
                || response.Content.Headers.ContentType?.MediaType != "application/x-git-upload-pack-advertisement")
                throw AccessFailed();

            // Only the service announcement and flush packet are needed. Large repositories
            // may advertise many megabytes of refs; never buffer their entire response.
            var expected = Encoding.ASCII.GetBytes("001e# service=git-upload-pack\n0000");
            var prefix = new byte[expected.Length];
            await using var stream = await response.Content.ReadAsStreamAsync(timeout.Token);
            await stream.ReadExactlyAsync(prefix, timeout.Token);
            if (!prefix.AsSpan().SequenceEqual(expected))
                throw AccessFailed();
        }
        catch (Exception ex) when (ex is HttpRequestException or IOException
            || ex is OperationCanceledException && !ct.IsCancellationRequested)
        {
            throw AccessFailed();
        }
    }

    private (string Origin, string Path) ValidateRepository(string provider, string serverUrl, string repositoryUrl)
    {
        var origin = ValidateServer(provider, serverUrl);
        if (!Uri.TryCreate(repositoryUrl, UriKind.Absolute, out var uri) || uri.GetLeftPart(UriPartial.Authority) != origin
            || uri.UserInfo.Length > 0 || uri.Query.Length > 0 || uri.Fragment.Length > 0)
            throw new SddpException("GIT_CONNECTION_HOST_MISMATCH", "Repository URL must belong to the selected Git server.");
        var path = uri.AbsolutePath.Trim('/');
        if (path.EndsWith(".git", StringComparison.Ordinal)) path = path[..^4];
        var parts = path.Split('/');
        if (parts.Length != 2 || parts.Any(x => x.Length == 0 || x is "." or ".." || x.Any(c => !char.IsAsciiLetterOrDigit(c) && c is not ('-' or '_' or '.'))))
            throw new SddpException("INVALID_REPOSITORY_URL", "Enter an owner/repository clone URL.");
        return (origin, path);
    }

    private static SddpException AccessFailed() =>
        new("GIT_ACCESS_FAILED", "Cannot verify anonymous repository access. Check repository visibility and server access.");

    public async Task CheckCredentialAsync(string provider, string serverUrl, string username, string token, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(username) || username.Contains(':') || token.Any(char.IsControl))
            throw new SddpException("INVALID_GIT_CREDENTIAL", "A Git username and a valid access token are required.");
        using var result = await Get(provider, ValidateServer(provider, serverUrl), "user", token, ct);
        if (result.RootElement.ValueKind != JsonValueKind.Object || !result.RootElement.TryGetProperty("login", out var login) || login.ValueKind != JsonValueKind.String || string.IsNullOrWhiteSpace(login.GetString()))
            throw new SddpException("GIT_AUTHENTICATION_FAILED", "The Git server did not confirm the credential identity.");
    }

    private async Task<JsonDocument> Get(string provider, string origin, string path, string? token, CancellationToken ct)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get,
            (provider == "GitHub" ? "https://api.github.com/" : origin + "/api/v1/") + path);
        request.Headers.UserAgent.ParseAdd("SDDP/0.5.0");
        request.Headers.Accept.ParseAdd("application/json");
        if (!string.IsNullOrEmpty(token)) request.Headers.Authorization = new AuthenticationHeaderValue(provider == "GitHub" ? "Bearer" : "token", token);
        try
        {
            using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct);
            if (!response.IsSuccessStatusCode)
                throw new SddpException("GIT_ACCESS_FAILED", "Cannot access the Git server. Check the repository, token and permissions.");
            await response.Content.LoadIntoBufferAsync(1024 * 1024, ct);
            return JsonDocument.Parse(await response.Content.ReadAsStringAsync(ct));
        }
        catch (Exception ex) when (ex is HttpRequestException or JsonException or TaskCanceledException && !ct.IsCancellationRequested)
        { throw new SddpException("GIT_ACCESS_FAILED", "Cannot verify repository access. Check the Git server and credentials."); }
    }
}
