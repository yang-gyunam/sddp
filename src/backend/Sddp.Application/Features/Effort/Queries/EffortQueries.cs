using MediatR;
using Sddp.Abstractions.DTOs;
using Sddp.Abstractions.Interfaces;
using Sddp.Abstractions.ValueObjects;
using Sddp.Application.Features.Effort;
using Sddp.Application.Requests;
using Sddp.Domain.Entities;

namespace Sddp.Application.Features.Effort.Queries;

/// <summary>
/// get (/user)
/// </summary>
public sealed record GetEffortAllocationsQuery(
    GlobalUniqueId TenantId,
    GlobalUniqueId ProjectId,
    DateOnly StartDate,
    DateOnly EndDate,
    List<GlobalUniqueId>? UserIds) : IQuery<List<EffortAllocationDto>>;

public sealed class GetEffortAllocationsQueryHandler : IRequestHandler<GetEffortAllocationsQuery, List<EffortAllocationDto>>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetEffortAllocationsQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<List<EffortAllocationDto>> Handle(GetEffortAllocationsQuery request, CancellationToken cancellationToken)
    {
        var repo = _unitOfWork.Repository<EffortAllocation>();

        var allocations = await (repo.FindAsync(
            a => a.TenantId == request.TenantId
                && a.ProjectId == request.ProjectId
                && a.AllocationDate >= request.StartDate
                && a.AllocationDate <= request.EndDate
                && (request.UserIds == null || request.UserIds.Count == 0 || request.UserIds.Contains(a.UserId)),
            cancellationToken)).ConfigureAwait(false);

        return allocations.Select(EffortMapping.MapToAllocationDto).ToList();
    }
}

/// <summary>
/// log get (/user)
/// </summary>
public sealed record GetWorklogsQuery(
    GlobalUniqueId TenantId,
    GlobalUniqueId ProjectId,
    DateOnly StartDate,
    DateOnly EndDate,
    List<GlobalUniqueId>? UserIds) : IQuery<List<WorklogDto>>;

public sealed class GetWorklogsQueryHandler : IRequestHandler<GetWorklogsQuery, List<WorklogDto>>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetWorklogsQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<List<WorklogDto>> Handle(GetWorklogsQuery request, CancellationToken cancellationToken)
    {
        var repo = _unitOfWork.Repository<Worklog>();

        var worklogs = await (repo.FindAsync(
            w => w.TenantId == request.TenantId
                && w.ProjectId == request.ProjectId
                && w.WorkDate >= request.StartDate
                && w.WorkDate <= request.EndDate
                && (request.UserIds == null || request.UserIds.Count == 0 || request.UserIds.Contains(w.UserId)),
            cancellationToken)).ConfigureAwait(false);

        return worklogs.Select(EffortMapping.MapToWorklogDto).ToList();
    }
}

/// <summary>
/// workday get ()
/// </summary>
public sealed record GetWorkingDaysQuery(
    GlobalUniqueId TenantId,
    GlobalUniqueId ProjectId,
    DateOnly StartDate,
    DateOnly EndDate) : IQuery<List<WorkingDayDto>>;

public sealed class GetWorkingDaysQueryHandler : IRequestHandler<GetWorkingDaysQuery, List<WorkingDayDto>>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetWorkingDaysQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<List<WorkingDayDto>> Handle(GetWorkingDaysQuery request, CancellationToken cancellationToken)
    {
        var repo = _unitOfWork.Repository<WorkingDay>();

        var workingDays = await (repo.FindAsync(
            wd => wd.TenantId == request.TenantId
                && wd.ProjectId == request.ProjectId
                && wd.WorkDate >= request.StartDate
                && wd.WorkDate <= request.EndDate,
            cancellationToken)).ConfigureAwait(false);

        return workingDays.Select(EffortMapping.MapToWorkingDayDto).ToList();
    }
}

/// <summary>
/// get
/// </summary>
public sealed record GetMembersSummaryQuery(
    GlobalUniqueId TenantId,
    GlobalUniqueId ProjectId,
    DateOnly StartDate,
    DateOnly EndDate) : IQuery<List<MemberEffortSummaryDto>>;

public sealed class GetMembersSummaryQueryHandler : IRequestHandler<GetMembersSummaryQuery, List<MemberEffortSummaryDto>>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetMembersSummaryQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<List<MemberEffortSummaryDto>> Handle(GetMembersSummaryQuery request, CancellationToken cancellationToken)
    {
        var allocationRepo = _unitOfWork.Repository<EffortAllocation>();
        var worklogRepo = _unitOfWork.Repository<Worklog>();
        var userRepo = _unitOfWork.Repository<User>();
        var requirementRepo = _unitOfWork.Repository<Requirement>();
        var specRepo = _unitOfWork.Repository<Spec>();
        var glossaryRepo = _unitOfWork.Repository<GlossaryTerm>();
        var artifactRepo = _unitOfWork.Repository<ArtifactTracking>();

        var startDateTime = DateTime.SpecifyKind(request.StartDate.ToDateTime(TimeOnly.MinValue), DateTimeKind.Utc);
        var endDateTime = DateTime.SpecifyKind(request.EndDate.ToDateTime(TimeOnly.MaxValue), DateTimeKind.Utc);
        var startTimestamp = Timestamp.FromDateTime(startDateTime);
        var endTimestamp = Timestamp.FromDateTime(endDateTime);

        var allocations = await (allocationRepo.FindAsync(
            a => a.TenantId == request.TenantId
                && a.ProjectId == request.ProjectId
                && a.AllocationDate >= request.StartDate
                && a.AllocationDate <= request.EndDate,
            cancellationToken)).ConfigureAwait(false);

        var worklogs = await (worklogRepo.FindAsync(
            w => w.TenantId == request.TenantId
                && w.ProjectId == request.ProjectId
                && w.WorkDate >= request.StartDate
                && w.WorkDate <= request.EndDate,
            cancellationToken)).ConfigureAwait(false);

        var requirements = await (requirementRepo.FindAsync(
            r => r.TenantId == request.TenantId
                && r.ProjectId == request.ProjectId
                && r.CreatedAt >= startTimestamp
                && r.CreatedAt <= endTimestamp,
            cancellationToken)).ConfigureAwait(false);

        var specs = await (specRepo.FindAsync(
            s => s.TenantId == request.TenantId
                && s.ProjectId == request.ProjectId
                && s.CreatedAt >= startTimestamp
                && s.CreatedAt <= endTimestamp,
            cancellationToken)).ConfigureAwait(false);

        var glossaryTerms = await (glossaryRepo.FindAsync(
            g => g.TenantId == request.TenantId
                && g.ProjectId == request.ProjectId
                && g.CreatedAt >= startTimestamp
                && g.CreatedAt <= endTimestamp,
            cancellationToken)).ConfigureAwait(false);

        var artifacts = await (artifactRepo.FindAsync(
            a => a.TenantId == request.TenantId
                && a.ProjectId == request.ProjectId
                && a.CreatedAt >= startTimestamp
                && a.CreatedAt <= endTimestamp,
            cancellationToken)).ConfigureAwait(false);

        var requirementCounts = requirements
            .GroupBy(r => r.CreatedBy)
            .ToDictionary(g => g.Key, g => g.Count());

        var specCounts = specs
            .GroupBy(s => s.CreatedBy)
            .ToDictionary(g => g.Key, g => g.Count());

        var glossaryCounts = glossaryTerms
            .GroupBy(g => g.CreatedBy)
            .ToDictionary(g => g.Key, g => g.Count());

        var artifactCounts = artifacts
            .GroupBy(a => a.CreatedBy)
            .ToDictionary(g => g.Key, g => g.Count());

        var userIds = allocations.Select(a => a.UserId)
            .Union(worklogs.Select(w => w.UserId))
            .Union(requirementCounts.Keys)
            .Union(specCounts.Keys)
            .Union(glossaryCounts.Keys)
            .Union(artifactCounts.Keys)
            .Distinct()
            .ToList();

        var results = new List<MemberEffortSummaryDto>();

        foreach (var userId in userIds)
        {
            var user = await (userRepo.GetByIdAsync(userId, cancellationToken)).ConfigureAwait(false);
            if (user == null) continue;

            var totalAllocated = allocations
                .Where(a => a.UserId == userId)
                .Sum(a => a.AllocatedHours);

            var totalSpent = worklogs
                .Where(w => w.UserId == userId)
                .Sum(w => w.SpentHours);

            var remaining = totalAllocated - totalSpent;
            var utilizationRate = totalAllocated > 0
                ? Math.Round((totalSpent / totalAllocated) * 100, 1)
                : 0;

            results.Add(new MemberEffortSummaryDto
            {
                UserId = userId.ToGuid(),
                UserName = user.DisplayName ?? user.Username ?? "Unknown",
                UserEmail = user.Email ?? "",
                AvatarUrl = user.AvatarUrl,
                Role = user.UserRoles?.FirstOrDefault()?.Role?.Name ?? "",
                TotalAllocated = totalAllocated,
                TotalSpent = totalSpent,
                Remaining = remaining,
                UtilizationRate = utilizationRate,
                RequirementsCreated = requirementCounts.GetValueOrDefault(userId),
                SpecsCreated = specCounts.GetValueOrDefault(userId),
                GlossaryTermsCreated = glossaryCounts.GetValueOrDefault(userId),
                ArtifactsCreated = artifactCounts.GetValueOrDefault(userId)
            });
        }

        return results.OrderBy(m => m.UserName).ToList();
    }
}

/// <summary>
/// get
/// </summary>
public sealed record GetMemberDailyEffortQuery(
    GlobalUniqueId TenantId,
    GlobalUniqueId ProjectId,
    GlobalUniqueId UserId,
    DateOnly StartDate,
    DateOnly EndDate) : IQuery<List<DailyEffortDto>>;

public sealed class GetMemberDailyEffortQueryHandler : IRequestHandler<GetMemberDailyEffortQuery, List<DailyEffortDto>>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetMemberDailyEffortQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<List<DailyEffortDto>> Handle(GetMemberDailyEffortQuery request, CancellationToken cancellationToken)
    {
        var allocationRepo = _unitOfWork.Repository<EffortAllocation>();
        var worklogRepo = _unitOfWork.Repository<Worklog>();
        var workingDayRepo = _unitOfWork.Repository<WorkingDay>();

        var allocations = (await (allocationRepo.FindAsync(
            a => a.TenantId == request.TenantId
                && a.ProjectId == request.ProjectId
                && a.UserId == request.UserId
                && a.AllocationDate >= request.StartDate
                && a.AllocationDate <= request.EndDate,
            cancellationToken)).ConfigureAwait(false)).ToDictionary(a => a.AllocationDate);

        var worklogs = (await (worklogRepo.FindAsync(
            w => w.TenantId == request.TenantId
                && w.ProjectId == request.ProjectId
                && w.UserId == request.UserId
                && w.WorkDate >= request.StartDate
                && w.WorkDate <= request.EndDate,
            cancellationToken)).ConfigureAwait(false)).GroupBy(w => w.WorkDate).ToDictionary(g => g.Key, g => g.ToList());

        var workingDays = (await (workingDayRepo.FindAsync(
            wd => wd.TenantId == request.TenantId
                && wd.ProjectId == request.ProjectId
                && wd.WorkDate >= request.StartDate
                && wd.WorkDate <= request.EndDate,
            cancellationToken)).ConfigureAwait(false)).ToDictionary(wd => wd.WorkDate);

        var conflicts = await (EffortQueryHelpers.GetUserConflictsAsync(
            _unitOfWork,
            request.TenantId,
            request.UserId,
            request.StartDate,
            request.EndDate,
            cancellationToken)).ConfigureAwait(false);
        var conflictDates = conflicts.ToDictionary(c => DateOnly.Parse(c.Date), c => c);

        var results = new List<DailyEffortDto>();
        var currentDate = request.StartDate;

        while (currentDate <= request.EndDate)
        {
            var allocation = allocations.GetValueOrDefault(currentDate);
            var dayWorklogs = worklogs.GetValueOrDefault(currentDate) ?? new List<Worklog>();
            var workingDay = workingDays.GetValueOrDefault(currentDate);
            var conflict = conflictDates.GetValueOrDefault(currentDate);

            var isWeekend = currentDate.DayOfWeek == DayOfWeek.Saturday ||
                           currentDate.DayOfWeek == DayOfWeek.Sunday;

            var dayType = workingDay?.DayType ?? (isWeekend ? "offday" : "workday");
            var isWorkingDay = dayType == "workday" || dayType == "exception";

            results.Add(new DailyEffortDto
            {
                Date = currentDate.ToString("yyyy-MM-dd"),
                AllocatedHours = allocation?.AllocatedHours ?? 0,
                SpentHours = dayWorklogs.Sum(w => w.SpentHours),
                IsWorkingDay = isWorkingDay,
                WorkingDayType = dayType,
                Worklogs = dayWorklogs.Select(EffortMapping.MapToWorklogDto).ToList(),
                HasConflict = conflict != null,
                ConflictingProjects = conflict?.Projects.Select(p => p.ProjectName).ToList()
            });

            currentDate = currentDate.AddDays(1);
        }

        return results;
    }
}

/// <summary>
/// get ()
/// </summary>
public sealed record GetMemberOwnershipQuery(
    GlobalUniqueId TenantId,
    GlobalUniqueId ProjectId,
    GlobalUniqueId UserId,
    string? Filter,
    string? Query,
    int Page,
    int PageSize) : IQuery<MemberOwnershipPageDto>;

public sealed class GetMemberOwnershipQueryHandler : IRequestHandler<GetMemberOwnershipQuery, MemberOwnershipPageDto>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetMemberOwnershipQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<MemberOwnershipPageDto> Handle(GetMemberOwnershipQuery request, CancellationToken cancellationToken)
    {
        // Requirement+Spec+Glossary+Artifact 4 aggregated → in-memory
        // IQueryable integration, project
        var requirementRepo = _unitOfWork.Repository<Requirement>();
        var specRepo = _unitOfWork.Repository<Spec>();
        var glossaryRepo = _unitOfWork.Repository<GlossaryTerm>();
        var artifactRepo = _unitOfWork.Repository<ArtifactTracking>();

        var normalizedFilter = NormalizeOwnershipFilter(request.Filter);
        var normalizedQuery = string.IsNullOrWhiteSpace(request.Query) ? null : request.Query.Trim();
        var requestedPage = request.Page < 1 ? 1 : request.Page;
        var safePageSize = request.PageSize is < 1 or > 100 ? 20 : request.PageSize;

        var requirements = await (requirementRepo.FindAsync(
            r => r.TenantId == request.TenantId
                && r.ProjectId == request.ProjectId
                && r.CreatedBy == request.UserId
                && r.ValidTo == null,
            cancellationToken)).ConfigureAwait(false);

        var specs = await (specRepo.FindAsync(
            s => s.TenantId == request.TenantId
                && s.ProjectId == request.ProjectId
                && s.ValidTo == null,
            cancellationToken)).ConfigureAwait(false);

        var ownedSpecs = specs
            .Where(s => IsSpecOwner(s, request.UserId.ToGuid()))
            .ToList();

        var glossaryTerms = await (glossaryRepo.FindAsync(
            g => g.TenantId == request.TenantId
                && g.ProjectId == request.ProjectId
                && g.DefinedBy == request.UserId
                && g.ValidTo == null,
            cancellationToken)).ConfigureAwait(false);

        var ownedSpecIds = ownedSpecs.Select(s => s.Id).ToHashSet();
        var specCodeMap = specs.ToDictionary(s => s.Id, s => s.Code);

        var artifacts = await (artifactRepo.FindAsync(
            a => a.TenantId == request.TenantId
                && a.ProjectId == request.ProjectId,
            cancellationToken)).ConfigureAwait(false);

        var ownedArtifacts = artifacts
            .Where(a => a.CreatedBy == request.UserId || ownedSpecIds.Contains(a.SpecId))
            .ToList();

        var items = new List<OwnershipItem>();

        items.AddRange(requirements.Select(r => new OwnershipItem(
            Type: "requirements",
            Id: r.Id.ToGuid(),
            Title: $"{r.Code} · {r.Title}",
            Subtitle: $"{r.Level} · {r.Status}",
            ArtifactPath: null,
            SpecId: null,
            SpecCode: null,
            UpdatedAt: r.UpdatedAt
        )));

        items.AddRange(ownedSpecs.Select(s => new OwnershipItem(
            Type: "specs",
            Id: s.Id.ToGuid(),
            Title: $"{s.Code} · {s.Title}",
            Subtitle: $"v{s.Version} · {s.Status}",
            ArtifactPath: null,
            SpecId: s.Id.ToGuid(),
            SpecCode: s.Code,
            UpdatedAt: s.UpdatedAt
        )));

        items.AddRange(glossaryTerms.Select(g => new OwnershipItem(
            Type: "glossary",
            Id: g.Id.ToGuid(),
            Title: g.Term,
            Subtitle: $"{g.Category} · {g.Status}",
            ArtifactPath: null,
            SpecId: null,
            SpecCode: null,
            UpdatedAt: g.UpdatedAt
        )));

        items.AddRange(ownedArtifacts.Select(a => new OwnershipItem(
            Type: "artifacts",
            Id: a.Id.ToGuid(),
            Title: a.EntityName,
            Subtitle: string.Join(" · ", new[]
            {
                a.ArtifactType.ToString(),
                specCodeMap.GetValueOrDefault(a.SpecId)
            }.Where(s => !string.IsNullOrWhiteSpace(s))),
            ArtifactPath: a.ArtifactPath,
            SpecId: a.SpecId.ToGuid(),
            SpecCode: specCodeMap.GetValueOrDefault(a.SpecId),
            UpdatedAt: a.UpdatedAt
        )));

        var filtered = items.Where(item =>
        {
            if (normalizedFilter != "all" && item.Type != normalizedFilter)
                return false;

            if (string.IsNullOrEmpty(normalizedQuery))
                return true;

            return ContainsIgnoreCase(item.Title, normalizedQuery)
                || ContainsIgnoreCase(item.Subtitle, normalizedQuery)
                || ContainsIgnoreCase(item.ArtifactPath, normalizedQuery)
                || ContainsIgnoreCase(item.SpecCode, normalizedQuery);
        });

        var ordered = filtered
            .OrderByDescending(item => item.UpdatedAt)
            .ToList();

        var totalCount = ordered.Count;
        var totalPages = totalCount == 0
            ? 1
            : (int)Math.Ceiling(totalCount / (double)safePageSize);
        var safePage = Math.Min(requestedPage, totalPages);
        var pageItems = ordered
            .Skip((safePage - 1) * safePageSize)
            .Take(safePageSize)
            .Select(item => new MemberOwnershipItemDto
            {
                Id = item.Id,
                Type = item.Type,
                Title = item.Title,
                Subtitle = item.Subtitle,
                ArtifactPath = item.ArtifactPath,
                SpecId = item.SpecId,
                SpecCode = item.SpecCode,
                UpdatedAt = item.UpdatedAt.ToIso8601()
            })
            .ToList();

        var counts = new MemberOwnershipCountsDto
        {
            Requirements = requirements.Count,
            Specs = ownedSpecs.Count,
            GlossaryTerms = glossaryTerms.Count,
            Artifacts = ownedArtifacts.Count,
            Total = requirements.Count + ownedSpecs.Count + glossaryTerms.Count + ownedArtifacts.Count
        };

        return new MemberOwnershipPageDto
        {
            UserId = request.UserId.ToGuid(),
            Filter = normalizedFilter,
            Query = normalizedQuery,
            Page = safePage,
            PageSize = safePageSize,
            TotalCount = totalCount,
            Counts = counts,
            Items = pageItems
        };
    }

    private static bool IsSpecOwner(Spec spec, Guid userId)
    {
        if (spec.CreatedBy.ToGuid() == userId)
            return true;

        return spec.IsOwnedBy(GlobalUniqueId.FromGuid(userId));
    }

    private static string NormalizeOwnershipFilter(string? filter)
    {
        return filter?.Trim().ToLowerInvariant() switch
        {
            "requirements" => "requirements",
            "specs" => "specs",
            "glossary" => "glossary",
            "artifacts" => "artifacts",
            _ => "all"
        };
    }

    private static bool ContainsIgnoreCase(string? value, string query)
    {
        return !string.IsNullOrEmpty(value)
            && value.Contains(query, StringComparison.OrdinalIgnoreCase);
    }

    private sealed record OwnershipItem(
        string Type,
        Guid Id,
        string Title,
        string? Subtitle,
        string? ArtifactPath,
        Guid? SpecId,
        string? SpecCode,
        Timestamp UpdatedAt);
}

/// <summary>
/// get
/// </summary>
public sealed record GetEffortConflictsQuery(
    GlobalUniqueId TenantId,
    GlobalUniqueId ProjectId,
    DateOnly StartDate,
    DateOnly EndDate) : IQuery<List<AllocationConflictDto>>;

public sealed class GetEffortConflictsQueryHandler : IRequestHandler<GetEffortConflictsQuery, List<AllocationConflictDto>>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetEffortConflictsQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<List<AllocationConflictDto>> Handle(GetEffortConflictsQuery request, CancellationToken cancellationToken)
    {
        var allocationRepo = _unitOfWork.Repository<EffortAllocation>();

        var projectAllocations = await (allocationRepo.FindAsync(
            a => a.TenantId == request.TenantId
                && a.ProjectId == request.ProjectId
                && a.AllocationDate >= request.StartDate
                && a.AllocationDate <= request.EndDate,
            cancellationToken)).ConfigureAwait(false);

        var userIds = projectAllocations.Select(a => a.UserId).Distinct().ToList();

        var conflicts = new List<AllocationConflictDto>();

        foreach (var userId in userIds)
        {
            var userConflicts = await (EffortQueryHelpers.GetUserConflictsAsync(
                _unitOfWork,
                request.TenantId,
                userId,
                request.StartDate,
                request.EndDate,
                cancellationToken)).ConfigureAwait(false);
            conflicts.AddRange(userConflicts);
        }

        return conflicts
            .DistinctBy(c => $"{c.UserId}_{c.Date}")
            .OrderBy(c => c.Date)
            .ThenBy(c => c.UserName)
            .ToList();
    }
}
