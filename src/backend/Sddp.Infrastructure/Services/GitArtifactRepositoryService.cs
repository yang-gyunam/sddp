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

    public GitArtifactRepositoryService(
        IUnitOfWork unitOfWork,
        IOptions<GitRepositoryOptions> options,
        ILogger<GitArtifactRepositoryService> logger)
    {
        _unitOfWork = unitOfWork;
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

        var repoPath = Path.Combine(_repoRoot, tenantId.ToString(), projectId.ToString());

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
        Directory.CreateDirectory(_repoRoot);

        var repoPath = context.RepoPath;
        var gitDir = Path.Combine(repoPath, ".git");
        var repoExists = Directory.Exists(gitDir);

        if (!repoExists)
        {
            await (CloneRepositoryAsync(context, cancellationToken)).ConfigureAwait(false);
            await (RecordSyncAsync(context, cancellationToken)).ConfigureAwait(false);
            return;
        }

        if (!ShouldSync(context))
            return;

        await (SyncRepositoryAsync(context, cancellationToken)).ConfigureAwait(false);
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

        if (!fullPath.StartsWith(rootFullPath, StringComparison.OrdinalIgnoreCase))
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

    private bool ShouldSync(ArtifactRepositoryContext context)
    {
        if (context.SyncIntervalMinutes <= 0)
            return false;

        if (context.LastSyncedAt is null)
            return true;

        return context.LastSyncedAt.Value.AddMinutes(context.SyncIntervalMinutes) <= DateTimeOffset.UtcNow;
    }

    private async Task CloneRepositoryAsync(
        ArtifactRepositoryContext context,
        CancellationToken cancellationToken)
    {
        var parentDir = Directory.GetParent(context.RepoPath)?.FullName;
        if (string.IsNullOrEmpty(parentDir))
            throw new InvalidOperationException("Invalid repository path");

        Directory.CreateDirectory(parentDir);

        var authUrl = BuildAuthenticatedUrl(context.RepoUrl);
        var maskedUrl = MaskUrl(authUrl);
        var branch = string.IsNullOrWhiteSpace(context.RepoBranch) ? "main" : context.RepoBranch;

        _logger.LogInformation("Cloning repository {RepoUrl} into {RepoPath}", maskedUrl, context.RepoPath);

        var args = $"clone --branch {Quote(branch)} --single-branch {Quote(authUrl)} {Quote(context.RepoPath)}";
        await (RunGitAsync(args, parentDir, cancellationToken)).ConfigureAwait(false);
    }

    private async Task SyncRepositoryAsync(
        ArtifactRepositoryContext context,
        CancellationToken cancellationToken)
    {
        var authUrl = BuildAuthenticatedUrl(context.RepoUrl);
        var maskedUrl = MaskUrl(authUrl);
        var branch = string.IsNullOrWhiteSpace(context.RepoBranch) ? "main" : context.RepoBranch;

        _logger.LogInformation("Syncing repository {RepoUrl} in {RepoPath}", maskedUrl, context.RepoPath);

        await (RunGitAsync($"-C {Quote(context.RepoPath)} remote set-url origin {Quote(authUrl)}",
            Directory.GetCurrentDirectory(),
            cancellationToken)).ConfigureAwait(false);

        await (RunGitAsync($"-C {Quote(context.RepoPath)} fetch origin {Quote(branch)}",
            Directory.GetCurrentDirectory(),
            cancellationToken)).ConfigureAwait(false);

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
            cancellationToken)).ConfigureAwait(false);
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

    private string BuildAuthenticatedUrl(string repoUrl)
    {
        if (string.IsNullOrWhiteSpace(_options.Token))
            return repoUrl;

        if (!repoUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            return repoUrl;

        var user = string.IsNullOrWhiteSpace(_options.Username) ? "oauth2" : _options.Username;
        var builder = new UriBuilder(repoUrl)
        {
            UserName = user,
            Password = _options.Token
        };
        return builder.Uri.ToString();
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

    private async Task RunGitAsync(string arguments, string workingDirectory, CancellationToken cancellationToken)
    {
        var result = await (TryRunGitAsync(arguments, workingDirectory, cancellationToken, throwOnError: true)).ConfigureAwait(false);
        _ = result;
    }

    private async Task<bool> TryRunGitAsync(
        string arguments,
        string workingDirectory,
        CancellationToken cancellationToken,
        bool throwOnError = false)
    {
        var maskedArgs = MaskGitArgs(arguments);
        var psi = new ProcessStartInfo("git", arguments)
        {
            WorkingDirectory = workingDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };
        psi.Environment["GIT_TERMINAL_PROMPT"] = "0";

        using var process = new Process { StartInfo = psi };
        process.Start();

        var stdoutTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
        var stderrTask = process.StandardError.ReadToEndAsync(cancellationToken);

        await (process.WaitForExitAsync(cancellationToken)).ConfigureAwait(false);

        var stdout = await (stdoutTask).ConfigureAwait(false);
        var stderr = await (stderrTask).ConfigureAwait(false);

        if (process.ExitCode == 0)
            return true;

        _logger.LogError("Git command failed: git {Args}\n{StdErr}", maskedArgs, stderr);

        if (throwOnError)
        {
            throw new InvalidOperationException(
                $"Git command failed (exit {process.ExitCode}): git {maskedArgs}\n{stderr}");
        }

        return false;
    }

    private string MaskGitArgs(string arguments)
    {
        if (string.IsNullOrWhiteSpace(arguments))
            return arguments;

        if (!string.IsNullOrWhiteSpace(_options.Token))
        {
            arguments = arguments.Replace(_options.Token, "****", StringComparison.Ordinal);
        }

        return arguments;
    }
}
