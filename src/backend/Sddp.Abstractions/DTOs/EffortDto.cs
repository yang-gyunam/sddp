namespace Sddp.Abstractions.DTOs;

// ============================================
// Effort Allocation DTOs
// ============================================

/// <summary>
/// Effort allocation response
/// </summary>
public record EffortAllocationDto
{
    public Guid Id { get; init; }
    public Guid UserId { get; init; }
    public Guid ProjectId { get; init; }
    public string Date { get; init; } = string.Empty; // YYYY-MM-DD
    public decimal AllocatedHours { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset UpdatedAt { get; init; }
    public Guid CreatedBy { get; init; }
    public Guid UpdatedBy { get; init; }
}

/// <summary>
/// Create/Update effort allocation request
/// </summary>
public record UpsertEffortAllocationRequest
{
    public Guid UserId { get; init; }
    public string Date { get; init; } = string.Empty;
    public decimal AllocatedHours { get; init; }
}

/// <summary>
/// Bulk allocation request
/// </summary>
public record BulkEffortAllocationRequest
{
    public List<UpsertEffortAllocationRequest> Allocations { get; init; } = new();
}

// ============================================
// Worklog DTOs
// ============================================

/// <summary>
/// Worklog response
/// </summary>
public record WorklogDto
{
    public Guid Id { get; init; }
    public Guid UserId { get; init; }
    public Guid ProjectId { get; init; }
    public string Date { get; init; } = string.Empty; // YYYY-MM-DD
    public decimal SpentHours { get; init; }
    public string? Note { get; init; }
    public Guid? TaskId { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset UpdatedAt { get; init; }
}

/// <summary>
/// Create worklog request
/// </summary>
public record CreateWorklogRequest
{
    public Guid UserId { get; init; }
    public string Date { get; init; } = string.Empty;
    public decimal SpentHours { get; init; }
    public string? Note { get; init; }
    public Guid? TaskId { get; init; }
}

/// <summary>
/// Update worklog request
/// </summary>
public record UpdateWorklogRequest
{
    public decimal SpentHours { get; init; }
    public string? Note { get; init; }
    public Guid? TaskId { get; init; }
}

// ============================================
// Working Day DTOs
// ============================================

/// <summary>
/// Working day response
/// </summary>
public record WorkingDayDto
{
    public Guid Id { get; init; }
    public Guid ProjectId { get; init; }
    public string Date { get; init; } = string.Empty; // YYYY-MM-DD
    public string Type { get; init; } = "workday"; // workday, offday, holiday, exception
    public string? Note { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset UpdatedAt { get; init; }
}

/// <summary>
/// Set working day request
/// </summary>
public record SetWorkingDayRequest
{
    public string Date { get; init; } = string.Empty;
    public string Type { get; init; } = "workday";
    public string? Note { get; init; }
}

/// <summary>
/// Bulk set working days request
/// </summary>
public record BulkWorkingDaysRequest
{
    public List<SetWorkingDayRequest> WorkingDays { get; init; } = new();
}

// ============================================
// Summary & Aggregation DTOs
// ============================================

/// <summary>
/// Member effort summary
/// </summary>
public record MemberEffortSummaryDto
{
    public Guid UserId { get; init; }
    public string UserName { get; init; } = string.Empty;
    public string UserEmail { get; init; } = string.Empty;
    public string? AvatarUrl { get; init; }
    public string Role { get; init; } = string.Empty;
    public decimal TotalAllocated { get; init; }
    public decimal TotalSpent { get; init; }
    public decimal Remaining { get; init; }
    public decimal UtilizationRate { get; init; } // percentage
    public int RequirementsCreated { get; init; }
    public int SpecsCreated { get; init; }
    public int GlossaryTermsCreated { get; init; }
    public int ArtifactsCreated { get; init; }
}

/// <summary>
/// Daily effort data for a member
/// </summary>
public record DailyEffortDto
{
    public string Date { get; init; } = string.Empty;
    public decimal AllocatedHours { get; init; }
    public decimal SpentHours { get; init; }
    public bool IsWorkingDay { get; init; }
    public string WorkingDayType { get; init; } = "workday";
    public List<WorklogDto> Worklogs { get; init; } = new();
    public bool HasConflict { get; init; }
    public List<string>? ConflictingProjects { get; init; }
}

/// <summary>
/// Allocation conflict information
/// </summary>
public record AllocationConflictDto
{
    public Guid UserId { get; init; }
    public string UserName { get; init; } = string.Empty;
    public string Date { get; init; } = string.Empty;
    public decimal TotalAllocated { get; init; }
    public List<ProjectAllocationDto> Projects { get; init; } = new();
}

/// <summary>
/// Project allocation in conflict
/// </summary>
public record ProjectAllocationDto
{
    public Guid ProjectId { get; init; }
    public string ProjectName { get; init; } = string.Empty;
    public decimal AllocatedHours { get; init; }
}

// ============================================
// Ownership DTOs
// ============================================

/// <summary>
/// Member-owned deliverables in a project
/// </summary>
public record MemberOwnershipDto
{
    public Guid UserId { get; init; }
    public List<OwnedRequirementDto> Requirements { get; init; } = new();
    public List<OwnedSpecDto> Specs { get; init; } = new();
    public List<OwnedGlossaryTermDto> GlossaryTerms { get; init; } = new();
    public List<OwnedArtifactDto> Artifacts { get; init; } = new();
}

public record OwnedRequirementDto
{
    public Guid Id { get; init; }
    public string Code { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public string Level { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public string CreatedAt { get; init; } = string.Empty;
    public string UpdatedAt { get; init; } = string.Empty;
}

public record OwnedSpecDto
{
    public Guid Id { get; init; }
    public string Code { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public string Version { get; init; } = string.Empty;
    public string? Owners { get; init; }
    public string CreatedAt { get; init; } = string.Empty;
    public string UpdatedAt { get; init; } = string.Empty;
}

public record OwnedGlossaryTermDto
{
    public Guid Id { get; init; }
    public string Term { get; init; } = string.Empty;
    public string Category { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public string CreatedAt { get; init; } = string.Empty;
    public string UpdatedAt { get; init; } = string.Empty;
}

public record OwnedArtifactDto
{
    public Guid Id { get; init; }
    public Guid SpecId { get; init; }
    public string? SpecCode { get; init; }
    public string ArtifactPath { get; init; } = string.Empty;
    public string ArtifactType { get; init; } = string.Empty;
    public string EntityName { get; init; } = string.Empty;
    public string CreatedAt { get; init; } = string.Empty;
    public string UpdatedAt { get; init; } = string.Empty;
}

public record MemberOwnershipCountsDto
{
    public int Requirements { get; init; }
    public int Specs { get; init; }
    public int GlossaryTerms { get; init; }
    public int Artifacts { get; init; }
    public int Total { get; init; }
}

public record MemberOwnershipItemDto
{
    public Guid Id { get; init; }
    public string Type { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public string? Subtitle { get; init; }
    public string? ArtifactPath { get; init; }
    public Guid? SpecId { get; init; }
    public string? SpecCode { get; init; }
    public string UpdatedAt { get; init; } = string.Empty;
}

public record MemberOwnershipPageDto
{
    public Guid UserId { get; init; }
    public string Filter { get; init; } = "all";
    public string? Query { get; init; }
    public int Page { get; init; }
    public int PageSize { get; init; }
    public int TotalCount { get; init; }
    public MemberOwnershipCountsDto Counts { get; init; } = new();
    public List<MemberOwnershipItemDto> Items { get; init; } = new();
}
