using Sddp.Abstractions.Base;
using Sddp.Abstractions.Enums;
using Sddp.Abstractions.Exceptions;
using Sddp.Abstractions.ValueObjects;

namespace Sddp.Domain.Entities;

/// <summary>
/// Project entity
/// Lifecycle: Planning → Active → Concluded → Archived
/// </summary>
public class Project : EntityBase
{
    /// <summary>
    /// Tenant ID
    /// </summary>
    public GlobalUniqueId TenantId { get; private set; }

    /// <summary>
    /// Project code
    /// </summary>
    public string Code { get; private set; } = string.Empty;

    /// <summary>
    /// Project name
    /// </summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>
    /// Project description
    /// </summary>
    public string? Description { get; private set; }

    /// <summary>
    /// Project lifecycle status
    /// </summary>
    public ProjectStatus Status { get; private set; } = ProjectStatus.Planning;

    /// <summary>
    /// Project owner ID
    /// </summary>
    public GlobalUniqueId? OwnerId { get; private set; }

    /// <summary>
    /// Git repository URL
    /// </summary>
    public string RepoUrl { get; private set; } = string.Empty;

    /// <summary>
    /// Git branch
    /// </summary>
    public string RepoBranch { get; private set; } = "main";

    /// <summary>
    /// Artifact root path
    /// </summary>
    public string ArtifactRootPath { get; private set; } = string.Empty;

    /// <summary>
    /// Synchronization interval in minutes
    /// </summary>
    public int SyncIntervalMinutes { get; private set; } = 60;

    /// <summary>
    /// Last synchronization time
    /// </summary>
    public Timestamp? LastSyncedAt { get; private set; }

    public GlobalUniqueId? GitConnectionId { get; private set; }
    public string? RepositoryVisibility { get; private set; }
    public void SelectGitConnection(GlobalUniqueId? id)
    {
        if (GitConnectionId == id) return;
        GitConnectionId = id;
        InvalidateRepositoryAccess();
    }
    public void InvalidateRepositoryAccess()
    { RepositoryCheckedAt = null; RepositoryVersion++; MarkAsModified(); }
    public void RecordRepositoryVisibility(bool isPrivate)
    { RepositoryVisibility = isPrivate ? "private" : "public"; }

    public int RepositoryVersion { get; private set; }
    public DateTimeOffset? RepositoryCheckedAt { get; private set; }
    public DateTimeOffset? RepositoryConfirmedAt { get; private set; }

    public void CheckRepositoryVersion(int expectedVersion)
    {
        if (RepositoryVersion != expectedVersion)
            throw new ConflictException("Repository settings changed. Refresh and review the current settings.");
    }

    public void RecordRepositoryCheck(DateTimeOffset now)
    {
        RepositoryCheckedAt = now;
        RepositoryVersion++;
        MarkAsModified();
    }

    public void ConfirmRepository(DateTimeOffset now)
    {
        if (RepositoryConfirmedAt.HasValue) return;
        if (string.IsNullOrWhiteSpace(RepoUrl) || RepositoryCheckedAt is not { } checkedAt
            || checkedAt < now.AddMinutes(-15))
            throw new ConflictException("Check the repository connection before confirming it. Connection checks are valid for 15 minutes.");
        RepositoryConfirmedAt = now;
        RepositoryVersion++;
        MarkAsModified();
    }

    public void RequireConfirmedRepository()
    {
        if (!RepositoryConfirmedAt.HasValue)
            throw new SddpException("REPOSITORY_CONFIRMATION_REQUIRED",
                "Confirm the Git repository in Settings → Project → General before saving file relationships.");
    }

    /// <summary>
    /// Project members
    /// </summary>
    public ICollection<ProjectMember> Members { get; private set; } = [];

    // Default constructor for EF Core
    private Project() { }

    public Project(
        GlobalUniqueId tenantId,
        string code,
        string name,
        string? description,
        GlobalUniqueId? ownerId,
        string repoUrl,
        string? repoBranch,
        string artifactRootPath,
        int syncIntervalMinutes)
    {
        TenantId = tenantId;
        Code = code;
        Name = name;
        Description = description;
        Status = ProjectStatus.Planning;
        OwnerId = ownerId;
        RepoUrl = repoUrl;
        RepoBranch = string.IsNullOrWhiteSpace(repoBranch) ? "main" : repoBranch.Trim();
        ArtifactRootPath = artifactRootPath;
        SyncIntervalMinutes = syncIntervalMinutes > 0 ? syncIntervalMinutes : 60;
    }

    // ========================================================================
    // Lifecycle state transitions
    // ========================================================================

    /// <summary>Initializes the project (Planning -> Active)</summary>
    public void Initialize()
    {
        if (!Status.CanInitialize())
            throw new InvalidOperationException($"Cannot initialize project in {Status} status");
        Status = ProjectStatus.Active;
        MarkAsModified();
    }

    /// <summary>Concludes the project (Active -> Concluded)</summary>
    public void Conclude()
    {
        if (!Status.CanConclude())
            throw new InvalidOperationException($"Cannot conclude project in {Status} status");
        Status = ProjectStatus.Concluded;
        MarkAsModified();
    }

    /// <summary>Reopens the project (Concluded -> Active)</summary>
    public void Reopen()
    {
        if (!Status.CanReopen())
            throw new InvalidOperationException($"Cannot reopen project in {Status} status");
        Status = ProjectStatus.Active;
        MarkAsModified();
    }

    /// <summary>Archives the project (Concluded -> Archived)</summary>
    public void Archive()
    {
        if (!Status.CanArchive())
            throw new InvalidOperationException($"Cannot archive project in {Status} status");
        Status = ProjectStatus.Archived;
        MarkAsModified();
    }

    /// <summary>Resets the status to Planning (used during data reset)</summary>
    public void ResetStatus()
    {
        Status = ProjectStatus.Planning;
        MarkAsModified();
    }

    // ========================================================================
    // Property updates
    // ========================================================================

    public void UpdateDetails(string name, string? description, GlobalUniqueId? ownerId)
    {
        if (Status.IsReadOnly())
            throw new InvalidOperationException($"Cannot update project in {Status} status");
        Name = name;
        Description = description;
        OwnerId = ownerId;
        MarkAsModified();
    }

    public void UpdateRepository(string repoUrl, string? repoBranch, string artifactRootPath, int syncIntervalMinutes)
    {
        if (Status.IsReadOnly())
            throw new ConflictException("Repository settings cannot be changed while the project is read-only.");
        var branch = string.IsNullOrWhiteSpace(repoBranch) ? RepoBranch : repoBranch.Trim();
        var changed = RepoUrl != repoUrl || RepoBranch != branch || ArtifactRootPath != artifactRootPath;
        if (changed && RepositoryConfirmedAt.HasValue)
            throw new ConflictException("The repository is confirmed. Changing its URL, branch or artifact root requires a repository migration.");
        RepoUrl = repoUrl;
        RepoBranch = branch;
        ArtifactRootPath = artifactRootPath;
        SyncIntervalMinutes = syncIntervalMinutes > 0 ? syncIntervalMinutes : SyncIntervalMinutes;
        if (changed)
        {
            RepositoryCheckedAt = null;
            LastSyncedAt = null;
        }
        RepositoryVersion++;
        MarkAsModified();
    }

    public void RecordSync()
    {
        LastSyncedAt = Timestamp.Now;
        MarkAsModified();
    }
}
