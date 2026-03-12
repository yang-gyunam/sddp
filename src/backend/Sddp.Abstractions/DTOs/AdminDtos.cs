namespace Sddp.Abstractions.DTOs;

public record ProjectDataResetResultDto(
    string ProjectId,
    string SnapshotId,
    int TotalRowsDeleted,
    Dictionary<string, int> DeletedTableCounts);

public record TenantDataResetResultDto(
    string TenantId,
    int ProjectsReset,
    int SnapshotsCreated,
    int TotalRowsDeleted);

public record ResetProjectDataDto(string ConfirmationCode);

public record ResetTenantDataDto(string ConfirmationToken);

public record ProjectLifecycleReasonDto(string? Reason);
