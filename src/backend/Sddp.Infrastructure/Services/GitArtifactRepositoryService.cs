using System.Diagnostics;
using System.Security.Cryptography;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Sddp.Abstractions.Interfaces;
using Sddp.Abstractions.ValueObjects;
using Sddp.Domain.Entities;

namespace Sddp.Infrastructure.Services;

public class GitArtifactRepositoryService : IArtifactRepositoryService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly GitRepositoryOptions _options;
    private readonly ILogger<GitArtifactRepositoryService> _logger;
    private readonly string _repoRoot;
    private readonly IGitRepositoryAccess _access;

    public GitArtifactRepositoryService(
        IUnitOfWork unitOfWork,
        IOptions<GitRepositoryOptions> options,
        ILogger<GitArtifactRepositoryService> logger, IGitRepositoryAccess access)
    {
        _unitOfWork = unitOfWork;
        _access = access;
        _options = options.Value;
        _logger = logger;
        _repoRoot = Path.GetFullPath(_options.RepoRoot, Directory.GetCurrentDirectory());
    }

    public async Task<ArtifactRepositoryContext> GetProjectContextAsync(
        GlobalUniqueId tenantId,
        GlobalUniqueId projectId,
        CancellationToken cancellationToken = default)
    {
        var projectRepo = _unitOfWork.Repository<Project>();
        var project = await (projectRepo.GetByIdAsync(projectId, cancellationToken)).ConfigureAwait(false);

        if (project == null || project.TenantId != tenantId || !project.IsActive)
            throw new InvalidOperationException("Project not found or inactive");

        if (string.IsNullOrWhiteSpace(project.RepoUrl))
            throw new InvalidOperationException("Project repository URL is not configured");

        // A draft may point to another remote/branch. Never reuse its predecessor's checkout.
        var identity = Convert.ToHexString(SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(
            project.RepoUrl + "\n" + project.RepoBranch))).ToLowerInvariant()[..16];
        var repoPath = Path.Combine(_repoRoot, tenantId.ToString(), projectId.ToString(), identity);

        return new ArtifactRepositoryContext(
            TenantId: tenantId,
            ProjectId: projectId,
            RepoPath: repoPath,
            RepoUrl: project.RepoUrl,
            RepoBranch: project.RepoBranch,
            ArtifactRootPath: project.ArtifactRootPath,
            SyncIntervalMinutes: project.SyncIntervalMinutes,
            LastSyncedAt: project.LastSyncedAt?.ToDateTimeOffset());
    }

    public async Task EnsureProjectSyncedAsync(
        ArtifactRepositoryContext context,
        CancellationToken cancellationToken = default)
    {
        var project = await _unitOfWork.Repository<Project>().GetByIdAsync(context.ProjectId, cancellationToken);
        if (project is null || project.TenantId != context.TenantId || !project.IsActive || project.RepoUrl != context.RepoUrl)
            throw new InvalidOperationException("Project repository changed. Reload before browsing.");
        var remote = await _access.ResolveForReadAsync(context.TenantId, context.ProjectId, context.RepoUrl, project.GitConnectionId, cancellationToken);
        Directory.CreateDirectory(_repoRoot);

        var repoPath = context.RepoPath;
        var gitDir = Path.Combine(repoPath, ".git");
        var repoExists = Directory.Exists(gitDir);

        if (!repoExists)
        {
            await (CloneRepositoryAsync(context, remote, cancellationToken)).ConfigureAwait(false);
            await (RecordSyncAsync(context, cancellationToken)).ConfigureAwait(false);
            return;
        }

        // Remove credentials left by older installations even when a fetch is not due.
        await RunGitAsync($"-C {Quote(context.RepoPath)} remote set-url origin {Quote(context.RepoUrl)}", _repoRoot, cancellationToken);
        if (!ShouldSync(context))
            return;

        await (SyncRepositoryAsync(context, remote, cancellationToken)).ConfigureAwait(false);
        await (RecordSyncAsync(context, cancellationToken)).ConfigureAwait(false);
    }

    public async Task<ArtifactFileInfo> GetArtifactFileInfoAsync(
        ArtifactRepositoryContext context,
        string artifactPath,
        CancellationToken cancellationToken = default)
    {
        var root = Path.Combine(context.RepoPath, context.ArtifactRootPath ?? string.Empty);
        var rootFullPath = Path.GetFullPath(root);

        var sanitizedPath = artifactPath.TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        var combinedPath = Path.Combine(rootFullPath, sanitizedPath);
        var fullPath = Path.GetFullPath(combinedPath);

        if (!PathGuard.IsContainedIn(fullPath, rootFullPath))
        {
            return new ArtifactFileInfo(false, null, fullPath);
        }

        if (!File.Exists(fullPath))
        {
            return new ArtifactFileInfo(false, null, fullPath);
        }

        var hash = await (ComputeHashAsync(fullPath, cancellationToken)).ConfigureAwait(false);
        return new ArtifactFileInfo(true, hash, fullPath);
    }

    public async Task<ArtifactFileContent?> GetArtifactContentAsync(
        ArtifactRepositoryContext context,
        string artifactPath,
        CancellationToken cancellationToken = default)
    {
        var fileInfo = await (GetArtifactFileInfoAsync(context, artifactPath, cancellationToken)).ConfigureAwait(false);
        if (!fileInfo.Exists)
        {
            return null;
        }

        var length = new FileInfo(fileInfo.FullPath).Length;
        if (length > _options.MaxArtifactContentBytes)
        {
            _logger.LogWarning(
                "Refused to read artifact {Path}: {Length} bytes exceeds the {Max} byte ceiling",
                artifactPath, length, _options.MaxArtifactContentBytes);
            return new ArtifactFileContent(
                null, null, length, IsBinary: false,
                TooLarge: true, MaxBytes: _options.MaxArtifactContentBytes);
        }

        var bytes = await (File.ReadAllBytesAsync(fileInfo.FullPath, cancellationToken)).ConfigureAwait(false);

        // A generated artifact is text, but nothing stops a path from pointing at a binary.
        // Say so rather than handing back mojibake the editor would render as garbage.
        if (IsBinary(bytes))
        {
            return new ArtifactFileContent(null, fileInfo.Hash, bytes.LongLength, IsBinary: true);
        }

        var content = new StreamReader(new MemoryStream(bytes), detectEncodingFromByteOrderMarks: true).ReadToEnd();
        return new ArtifactFileContent(content, fileInfo.Hash, bytes.LongLength, IsBinary: false);
    }

    private static bool IsBinary(byte[] bytes)
    {
        var sampleLength = Math.Min(bytes.Length, 8000);
        for (var i = 0; i < sampleLength; i++)
        {
            if (bytes[i] == 0)
                return true;
        }

        return false;
    }

    private bool ShouldSync(ArtifactRepositoryContext context)
    {
        if (context.SyncIntervalMinutes <= 0)
            return false;

        if (context.LastSyncedAt is null)
            return true;

        return context.LastSyncedAt.Value.AddMinutes(context.SyncIntervalMinutes) <= DateTimeOffset.UtcNow;
    }

    private async Task CloneRepositoryAsync(
        ArtifactRepositoryContext context, GitRemoteAccess remote,
        CancellationToken cancellationToken)
    {
        var parentDir = Directory.GetParent(context.RepoPath)?.FullName;
        if (string.IsNullOrEmpty(parentDir))
            throw new InvalidOperationException("Invalid repository path");

        Directory.CreateDirectory(parentDir);

        var authUrl = context.RepoUrl;
        var maskedUrl = MaskUrl(authUrl);
        var branch = string.IsNullOrWhiteSpace(context.RepoBranch) ? "main" : context.RepoBranch;

        _logger.LogInformation("Cloning repository {RepoUrl} into {RepoPath}", maskedUrl, context.RepoPath);

        var args = $"clone --branch {Quote(branch)} --single-branch {Quote(authUrl)} {Quote(context.RepoPath)}";
        await (RunGitAsync(args, parentDir, cancellationToken, remote)).ConfigureAwait(false);
    }

    private async Task SyncRepositoryAsync(
        ArtifactRepositoryContext context, GitRemoteAccess remote,
        CancellationToken cancellationToken)
    {
        var authUrl = context.RepoUrl;
        var maskedUrl = MaskUrl(authUrl);
        var branch = string.IsNullOrWhiteSpace(context.RepoBranch) ? "main" : context.RepoBranch;

        _logger.LogInformation("Syncing repository {RepoUrl} in {RepoPath}", maskedUrl, context.RepoPath);

        await (RunGitAsync($"-C {Quote(context.RepoPath)} remote set-url origin {Quote(authUrl)}",
            Directory.GetCurrentDirectory(),
            cancellationToken)).ConfigureAwait(false);

        await (RunGitAsync($"-C {Quote(context.RepoPath)} fetch origin {Quote(branch)}",
            Directory.GetCurrentDirectory(),
            cancellationToken, remote)).ConfigureAwait(false);

        var checkoutResult = await (TryRunGitAsync($"-C {Quote(context.RepoPath)} checkout {Quote(branch)}",
            Directory.GetCurrentDirectory(),
            cancellationToken)).ConfigureAwait(false);

        if (!checkoutResult)
        {
            await (RunGitAsync($"-C {Quote(context.RepoPath)} checkout -b {Quote(branch)} {Quote($"origin/{branch}")}",
                Directory.GetCurrentDirectory(),
                cancellationToken)).ConfigureAwait(false);
        }

        await (RunGitAsync($"-C {Quote(context.RepoPath)} pull --ff-only origin {Quote(branch)}",
            Directory.GetCurrentDirectory(),
            cancellationToken, remote)).ConfigureAwait(false);
    }

    private async Task RecordSyncAsync(
        ArtifactRepositoryContext context,
        CancellationToken cancellationToken)
    {
        var projectRepo = _unitOfWork.Repository<Project>();
        var project = await (projectRepo.GetByIdAsync(context.ProjectId, cancellationToken)).ConfigureAwait(false);
        if (project == null || project.TenantId != context.TenantId)
            return;

        project.RecordSync();
        await (_unitOfWork.SaveChangesAsync(cancellationToken)).ConfigureAwait(false);
    }

    private static string MaskUrl(string repoUrl)
    {
        if (!Uri.TryCreate(repoUrl, UriKind.Absolute, out var uri))
            return repoUrl;

        if (string.IsNullOrEmpty(uri.UserInfo))
            return repoUrl;

        var builder = new UriBuilder(uri)
        {
            UserName = "****",
            Password = "****"
        };
        return builder.Uri.ToString();
    }

    private static string Quote(string value)
    {
        return $"\"{value.Replace("\"", "\\\"")}\"";
    }

    private static async Task<string> ComputeHashAsync(string filePath, CancellationToken cancellationToken)
    {
        await using var stream = File.OpenRead(filePath);
        using var sha = SHA256.Create();
        var hashBytes = await (sha.ComputeHashAsync(stream, cancellationToken)).ConfigureAwait(false);
        return Convert.ToHexString(hashBytes).ToLowerInvariant();
    }

    private async Task RunGitAsync(string arguments, string workingDirectory, CancellationToken cancellationToken, GitRemoteAccess? remote = null)
    {
        var result = await (TryRunGitAsync(arguments, workingDirectory, cancellationToken, throwOnError: true, remote: remote)).ConfigureAwait(false);
        _ = result;
    }

    private async Task<bool> TryRunGitAsync(
        string arguments,
        string workingDirectory,
        CancellationToken cancellationToken,
        bool throwOnError = false, GitRemoteAccess? remote = null)
    {
        var psi = new ProcessStartInfo("git", arguments)
        {
            WorkingDirectory = workingDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };
        GitProcessCredentials.Configure(psi, remote);

        using var process = new Process { StartInfo = psi };
        process.Start();

        var stdoutTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
        var stderrTask = process.StandardError.ReadToEndAsync(cancellationToken);

        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(60));
        try { await process.WaitForExitAsync(timeout.Token); }
        finally { if (!process.HasExited) process.Kill(entireProcessTree: true); }

        var stdout = await (stdoutTask).ConfigureAwait(false);
        var stderr = await (stderrTask).ConfigureAwait(false);

        if (process.ExitCode == 0)
            return true;

        _logger.LogWarning("Git repository operation failed (exit {Code})", process.ExitCode);

        if (throwOnError)
        {
            throw new InvalidOperationException(
                "Git operation failed. Check repository access and credentials.");
        }

        return false;
    }

}
