using MediatR;
using Sddp.Abstractions.DTOs;
using Sddp.Abstractions.Enums;
using Sddp.Abstractions.Interfaces;
using Sddp.Abstractions.ValueObjects;
using Sddp.Application.Requests;
using Sddp.Application.Utilities;
using Sddp.Domain.Entities;

namespace Sddp.Application.Features.Requirements.Queries;

/// <summary>
/// requirement get ()
/// </summary>
public sealed record GetRequirementsQuery(
    GlobalUniqueId TenantId,
    GlobalUniqueId? ProjectId,
    int Page,
    int PageSize,
    RequirementLevel? Level,
    RequirementStatus? Status) : IQuery<RequirementPageDto>;

public sealed class GetRequirementsQueryHandler : IRequestHandler<GetRequirementsQuery, RequirementPageDto>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetRequirementsQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<RequirementPageDto> Handle(GetRequirementsQuery request, CancellationToken cancellationToken)
    {
        var requirementRepo = _unitOfWork.Repository<Requirement>();

        var (pagedRequirements, totalCount) = await (requirementRepo.FindPagedAsync(
            r => r.TenantId == request.TenantId
                && (!request.ProjectId.HasValue || r.ProjectId == request.ProjectId.Value)
                && r.ValidTo == null
                && (request.Level == null || r.Level == request.Level)
                && (request.Status == null || r.Status == request.Status),
            request.Page, request.PageSize,
            orderBy: r => r.Code,
            cancellationToken: cancellationToken)).ConfigureAwait(false);

        var totalPages = (int)Math.Ceiling((double)totalCount / request.PageSize);

        // Batch load children counts
        // Convert to Guid list to avoid Npgsql array type mapping failure with GlobalUniqueId?
        var parentGuids = pagedRequirements.Select(r => r.Id.ToGuid()).ToList();
        var allChildren = await (requirementRepo.FindAsync(
            c => c.ParentId.HasValue && parentGuids.Contains((Guid)c.ParentId.Value)
                && c.IsActive && c.ValidTo == null,
            cancellationToken)).ConfigureAwait(false);
        var childrenCountMap = allChildren
            .GroupBy(c => c.ParentId!.Value)
            .ToDictionary(g => g.Key, g => g.Count());

        var items = pagedRequirements.Select(r =>
        {
            var childrenCount = childrenCountMap.TryGetValue(r.Id, out var count) ? count : 0;
            return RequirementMapping.MapToDto(r, childrenCount);
        }).ToList();

        return new RequirementPageDto(
            Items: items,
            TotalCount: totalCount,
            Page: request.Page,
            PageSize: request.PageSize,
            TotalPages: totalPages);
    }
}

/// <summary>
/// requirement get (ID)
/// </summary>
public sealed record GetRequirementByIdQuery(
    GlobalUniqueId TenantId,
    GlobalUniqueId ProjectId,
    GlobalUniqueId RequirementId) : IQuery<RequirementDetailDto?>;

public sealed class GetRequirementByIdQueryHandler : IRequestHandler<GetRequirementByIdQuery, RequirementDetailDto?>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetRequirementByIdQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<RequirementDetailDto?> Handle(GetRequirementByIdQuery request, CancellationToken cancellationToken)
    {
        var requirementRepo = _unitOfWork.Repository<Requirement>();

        var requirement = await (requirementRepo.GetByIdAsync(request.RequirementId, cancellationToken)).ConfigureAwait(false);
        if (requirement is null
            || requirement.TenantId != request.TenantId
            || requirement.ProjectId != request.ProjectId
            || !requirement.IsActive)
        {
            return null;
        }

        return await (RequirementMapping.MapToDetailDtoAsync(requirement, _unitOfWork, cancellationToken)).ConfigureAwait(false);
    }
}

/// <summary>
/// requirement get ()
/// </summary>
public sealed record GetRequirementByCodeQuery(
    GlobalUniqueId TenantId,
    GlobalUniqueId ProjectId,
    string Code) : IQuery<RequirementDetailDto?>;

public sealed class GetRequirementByCodeQueryHandler : IRequestHandler<GetRequirementByCodeQuery, RequirementDetailDto?>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetRequirementByCodeQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<RequirementDetailDto?> Handle(GetRequirementByCodeQuery request, CancellationToken cancellationToken)
    {
        var requirementRepo = _unitOfWork.Repository<Requirement>();

        var requirements = await (requirementRepo.FindAsync(
            r => r.TenantId == request.TenantId
                && r.ProjectId == request.ProjectId
                && r.Code == request.Code
                && r.IsActive
                && r.ValidTo == null,
            cancellationToken)).ConfigureAwait(false);

        var requirement = requirements.FirstOrDefault();
        if (requirement is null)
        {
            return null;
        }

        return await (RequirementMapping.MapToDetailDtoAsync(requirement, _unitOfWork, cancellationToken)).ConfigureAwait(false);
    }
}

/// <summary>
/// requirement search
/// </summary>
public sealed record SearchRequirementsQuery(
    GlobalUniqueId TenantId,
    GlobalUniqueId? ProjectId,
    string Query,
    int Limit = 15) : IQuery<IEnumerable<RequirementSummaryDto>>;

public sealed class SearchRequirementsQueryHandler : IRequestHandler<SearchRequirementsQuery, IEnumerable<RequirementSummaryDto>>
{
    private readonly IUnitOfWork _unitOfWork;

    public SearchRequirementsQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<IEnumerable<RequirementSummaryDto>> Handle(SearchRequirementsQuery request, CancellationToken cancellationToken)
    {
        var requirementRepo = _unitOfWork.Repository<Requirement>();
        var queryLower = request.Query.ToLower();

        var matches = await (requirementRepo.FindAsync(
            r => r.TenantId == request.TenantId
                && (!request.ProjectId.HasValue || r.ProjectId == request.ProjectId.Value)
                && r.IsActive
                && r.ValidTo == null
                && (r.Code.ToLower().Contains(queryLower)
                    || r.Title.ToLower().Contains(queryLower)),
            cancellationToken)).ConfigureAwait(false);

        return matches
            .OrderBy(r => r.Code)
            .Take(request.Limit)
            .Select(RequirementMapping.MapToSummaryDto)
            .ToList();
    }
}

/// <summary>
/// requirement get
/// </summary>
public sealed record GetRequirementTreeQuery(
    GlobalUniqueId TenantId,
    GlobalUniqueId ProjectId) : IQuery<IEnumerable<RequirementTreeNodeDto>>;

public sealed class GetRequirementTreeQueryHandler : IRequestHandler<GetRequirementTreeQuery, IEnumerable<RequirementTreeNodeDto>>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetRequirementTreeQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<IEnumerable<RequirementTreeNodeDto>> Handle(GetRequirementTreeQuery request, CancellationToken cancellationToken)
    {
        var requirementRepo = _unitOfWork.Repository<Requirement>();

        var allRequirements = await (requirementRepo.FindAsync(
            r => r.TenantId == request.TenantId
                && r.ProjectId == request.ProjectId
                && r.IsActive
                && r.ValidTo == null,
            cancellationToken)).ConfigureAwait(false);

        var requirementList = allRequirements.ToList();

        var childrenMap = requirementList
            .Where(r => r.ParentId.HasValue)
            .GroupBy(r => r.ParentId!.Value)
            .ToDictionary(g => g.Key, g => g.OrderBy(r => r.Code).ToList());

        var roots = requirementList
            .Where(r => r.ParentId == null)
            .OrderBy(r => r.Code)
            .Select(r => RequirementMapping.BuildTreeNode(r, childrenMap))
            .ToList();

        return roots;
    }
}

/// <summary>
/// requirement get
/// </summary>
public sealed record GetRequirementVersionHistoryQuery(
    GlobalUniqueId TenantId,
    GlobalUniqueId ProjectId,
    GlobalUniqueId RequirementId) : IQuery<List<RequirementVersionDto>>;

public sealed class GetRequirementVersionHistoryQueryHandler : IRequestHandler<GetRequirementVersionHistoryQuery, List<RequirementVersionDto>>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetRequirementVersionHistoryQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<List<RequirementVersionDto>> Handle(GetRequirementVersionHistoryQuery request, CancellationToken cancellationToken)
    {
        var requirementRepo = _unitOfWork.Repository<Requirement>();

        var currentRequirement = await (requirementRepo.GetByIdAsync(request.RequirementId, cancellationToken)).ConfigureAwait(false);
        if (currentRequirement is null
            || currentRequirement.TenantId != request.TenantId
            || currentRequirement.ProjectId != request.ProjectId
            || !currentRequirement.IsActive)
        {
            return [];
        }

        var allVersions = await (requirementRepo.FindAsync(
            r => r.TenantId == request.TenantId
                && r.ProjectId == request.ProjectId
                && r.Code == currentRequirement.Code
                && r.IsActive,
            cancellationToken)).ConfigureAwait(false);

        // Batch resolve user refs
        var userRepo = _unitOfWork.Repository<User>();
        var userIds = allVersions
            .SelectMany(r => new[] { r.OwnerUserId, (GlobalUniqueId?)r.CreatedBy, (GlobalUniqueId?)r.UpdatedBy })
            .Where(id => id.HasValue)
            .Select(id => id!.Value)
            .Distinct()
            .ToList();

        var userRefs = new Dictionary<GlobalUniqueId, UserRefDto>();
        foreach (var uid in userIds)
        {
            var userRef = await (UserRefHelper.ToUserRefAsync(userRepo, uid, cancellationToken)).ConfigureAwait(false);
            userRefs[uid] = userRef;
        }

        return allVersions
            .OrderByDescending(r => r.ValidFrom.ToDateTimeOffset())
            .Select(r => RequirementMapping.MapToVersionDto(
                r,
                r.OwnerUserId.HasValue && userRefs.TryGetValue(r.OwnerUserId.Value, out var ownerRef) ? ownerRef : null,
                userRefs.TryGetValue(r.CreatedBy, out var createdByRef) ? createdByRef : null,
                userRefs.TryGetValue(r.UpdatedBy, out var updatedByRef) ? updatedByRef : null))
            .ToList();
    }
}

/// <summary>
/// requirement get
/// </summary>
public sealed record GetRequirementChildrenQuery(
    GlobalUniqueId TenantId,
    GlobalUniqueId ProjectId,
    GlobalUniqueId ParentId) : IQuery<IEnumerable<RequirementDto>>;

public sealed class GetRequirementChildrenQueryHandler : IRequestHandler<GetRequirementChildrenQuery, IEnumerable<RequirementDto>>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetRequirementChildrenQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<IEnumerable<RequirementDto>> Handle(GetRequirementChildrenQuery request, CancellationToken cancellationToken)
    {
        var requirementRepo = _unitOfWork.Repository<Requirement>();

        var parent = await (requirementRepo.GetByIdAsync(request.ParentId, cancellationToken)).ConfigureAwait(false);
        if (parent is null
            || parent.TenantId != request.TenantId
            || parent.ProjectId != request.ProjectId
            || !parent.IsActive)
        {
            return [];
        }

        var children = await (requirementRepo.FindAsync(
            r => r.ParentId == request.ParentId && r.IsActive && r.ValidTo == null,
            cancellationToken)).ConfigureAwait(false);

        var childList = children.OrderBy(c => c.Code).ToList();

        // Batch load grandchildren counts
        // Convert to Guid list to avoid Npgsql array type mapping failure with GlobalUniqueId?
        var childGuids = childList.Select(c => c.Id.ToGuid()).ToList();
        var allGrandChildren = await (requirementRepo.FindAsync(
            c => c.ParentId.HasValue && childGuids.Contains((Guid)c.ParentId.Value)
                && c.IsActive && c.ValidTo == null,
            cancellationToken)).ConfigureAwait(false);
        var grandChildrenCountMap = allGrandChildren
            .GroupBy(c => c.ParentId!.Value)
            .ToDictionary(g => g.Key, g => g.Count());

        return childList.Select(child =>
        {
            var grandChildrenCount = grandChildrenCountMap.TryGetValue(child.Id, out var count) ? count : 0;
            return RequirementMapping.MapToDto(child, grandChildrenCount);
        }).ToList();
    }
}
