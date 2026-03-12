namespace Sddp.Abstractions.DTOs;

/// <summary>
/// Project snapshot summary (without snapshot_data)
/// </summary>
public sealed record ProjectSnapshotDto(
    string Id,
    string ProjectId,
    string Name,
    string? Description,
    string SnapshotType,
    string Status,
    Dictionary<string, int> TableCounts,
    long DataSizeBytes,
    UserRefDto CreatedBy,
    string CreatedAt);

/// <summary>
/// Request to create a new project snapshot
/// </summary>
public sealed record CreateProjectSnapshotDto(
    string Name,
    string? Description);
