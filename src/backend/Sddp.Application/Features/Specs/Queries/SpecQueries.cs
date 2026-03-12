using MediatR;
using Sddp.Abstractions.DTOs;
using Sddp.Abstractions.Enums;
using Sddp.Abstractions.Interfaces;
using Sddp.Abstractions.ValueObjects;
using Sddp.Application.Requests;
using Sddp.Domain.Entities;

namespace Sddp.Application.Features.Specs.Queries;

/// <summary>
/// spec get ()
/// </summary>
public sealed record GetSpecsQuery(
    GlobalUniqueId TenantId,
    GlobalUniqueId? ProjectId,
    int Page,
    int PageSize,
    SpecStatus? Status) : IQuery<SpecPageDto>;

public sealed class GetSpecsQueryHandler : IRequestHandler<GetSpecsQuery, SpecPageDto>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetSpecsQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<SpecPageDto> Handle(GetSpecsQuery request, CancellationToken cancellationToken)
    {
        var specRepo = _unitOfWork.Repository<Spec>();

        var (pagedSpecs, totalCount) = await (specRepo.FindPagedAsync(
            s => s.TenantId == request.TenantId
                && (!request.ProjectId.HasValue || s.ProjectId == request.ProjectId.Value)
                && s.ValidTo == null
                && (request.Status == null || s.Status == request.Status),
            request.Page, request.PageSize,
            orderBy: s => s.Code,
            cancellationToken: cancellationToken)).ConfigureAwait(false);

        var totalPages = (int)Math.Ceiling((double)totalCount / request.PageSize);
        var items = pagedSpecs.Select(SpecMapping.MapToDto).ToList();

        return new SpecPageDto(
            Items: items,
            TotalCount: totalCount,
            Page: request.Page,
            PageSize: request.PageSize,
            TotalPages: totalPages);
    }
}

/// <summary>
/// spec get (ID)
/// </summary>
public sealed record GetSpecByIdQuery(
    GlobalUniqueId TenantId,
    GlobalUniqueId ProjectId,
    GlobalUniqueId SpecId) : IQuery<SpecDetailDto?>;

public sealed class GetSpecByIdQueryHandler : IRequestHandler<GetSpecByIdQuery, SpecDetailDto?>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetSpecByIdQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<SpecDetailDto?> Handle(GetSpecByIdQuery request, CancellationToken cancellationToken)
    {
        var specRepo = _unitOfWork.Repository<Spec>();
        var spec = await (specRepo.GetByIdAsync(request.SpecId, cancellationToken)).ConfigureAwait(false);
        if (spec is null
            || spec.TenantId != request.TenantId
            || spec.ProjectId != request.ProjectId
            || !spec.IsActive)
        {
            return null;
        }

        return await (SpecMapping.MapToDetailDtoAsync(spec, _unitOfWork, cancellationToken)).ConfigureAwait(false);
    }
}

/// <summary>
/// spec get ()
/// </summary>
public sealed record GetSpecByCodeQuery(
    GlobalUniqueId TenantId,
    GlobalUniqueId ProjectId,
    string Code) : IQuery<SpecDetailDto?>;

public sealed class GetSpecByCodeQueryHandler : IRequestHandler<GetSpecByCodeQuery, SpecDetailDto?>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetSpecByCodeQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<SpecDetailDto?> Handle(GetSpecByCodeQuery request, CancellationToken cancellationToken)
    {
        var specRepo = _unitOfWork.Repository<Spec>();

        var specs = await (specRepo.FindAsync(
            s => s.TenantId == request.TenantId
                && s.ProjectId == request.ProjectId
                && s.Code == request.Code
                && s.IsActive
                && s.ValidTo == null,
            cancellationToken)).ConfigureAwait(false);

        var spec = specs.FirstOrDefault();
        if (spec is null)
        {
            return null;
        }

        return await (SpecMapping.MapToDetailDtoAsync(spec, _unitOfWork, cancellationToken)).ConfigureAwait(false);
    }
}

/// <summary>
/// spec search
/// </summary>
public sealed record SearchSpecsQuery(
    GlobalUniqueId TenantId,
    GlobalUniqueId? ProjectId,
    string Query,
    int Limit = 15) : IQuery<IEnumerable<SpecSummaryDto>>;

public sealed class SearchSpecsQueryHandler : IRequestHandler<SearchSpecsQuery, IEnumerable<SpecSummaryDto>>
{
    private readonly IUnitOfWork _unitOfWork;

    public SearchSpecsQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<IEnumerable<SpecSummaryDto>> Handle(SearchSpecsQuery request, CancellationToken cancellationToken)
    {
        var specRepo = _unitOfWork.Repository<Spec>();
        var queryLower = request.Query.ToLower();

        var matches = await (specRepo.FindAsync(
            s => s.TenantId == request.TenantId
                && (!request.ProjectId.HasValue || s.ProjectId == request.ProjectId.Value)
                && s.IsActive
                && s.ValidTo == null
                && (s.Code.ToLower().Contains(queryLower)
                    || s.Title.ToLower().Contains(queryLower)),
            cancellationToken)).ConfigureAwait(false);

        return matches
            .OrderBy(s => s.Code)
            .Take(request.Limit)
            .Select(SpecMapping.MapToSummaryDto)
            .ToList();
    }
}

/// <summary>
/// spec SignOff get
/// </summary>
public sealed record GetSpecSignOffsQuery(
    GlobalUniqueId TenantId,
    GlobalUniqueId ProjectId,
    GlobalUniqueId SpecId) : IQuery<List<SignOffDto>>;

public sealed class GetSpecSignOffsQueryHandler : IRequestHandler<GetSpecSignOffsQuery, List<SignOffDto>>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetSpecSignOffsQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<List<SignOffDto>> Handle(GetSpecSignOffsQuery request, CancellationToken cancellationToken)
    {
        var specRepo = _unitOfWork.Repository<Spec>();
        var signOffRepo = _unitOfWork.Repository<SignOff>();
        var userRepo = _unitOfWork.Repository<User>();

        var spec = await (specRepo.GetByIdAsync(request.SpecId, cancellationToken)).ConfigureAwait(false);
        if (spec is null
            || spec.TenantId != request.TenantId
            || spec.ProjectId != request.ProjectId
            || !spec.IsActive)
        {
            return [];
        }

        var signOffs = await (signOffRepo.FindAsync(
            s => s.SpecId == request.SpecId
                && s.TenantId == request.TenantId
                && s.ProjectId == request.ProjectId
                && s.IsActive,
            cancellationToken)).ConfigureAwait(false);

        var stakeholderIds = signOffs.Select(s => s.StakeholderId).Distinct().ToList();
        var stakeholders = await (userRepo.FindAsync(
            u => stakeholderIds.Contains(u.Id),
            cancellationToken)).ConfigureAwait(false);
        var userMap = stakeholders.ToDictionary(u => u.Id, u => new UserRefDto(u.Id.ToString(), u.DisplayName ?? u.Username, u.AvatarUrl));

        var result = signOffs
            .OrderBy(s => s.CreatedAt.ToDateTimeOffset())
            .Select(s => SpecMapping.MapToSignOffDto(
                s,
                userMap.TryGetValue(s.StakeholderId, out var userRef) ? userRef : new UserRefDto(s.StakeholderId.ToString(), "Unknown", null)))
            .ToList();

        return result;
    }
}

/// <summary>
/// spec SignOff get
/// </summary>
public sealed record GetSpecSignOffSummaryQuery(
    GlobalUniqueId TenantId,
    GlobalUniqueId ProjectId,
    GlobalUniqueId SpecId) : IQuery<SignOffSummaryDto>;

public sealed class GetSpecSignOffSummaryQueryHandler : IRequestHandler<GetSpecSignOffSummaryQuery, SignOffSummaryDto>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetSpecSignOffSummaryQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<SignOffSummaryDto> Handle(GetSpecSignOffSummaryQuery request, CancellationToken cancellationToken)
    {
        var specRepo = _unitOfWork.Repository<Spec>();
        var signOffRepo = _unitOfWork.Repository<SignOff>();
        var userRepo = _unitOfWork.Repository<User>();

        var spec = await (specRepo.GetByIdAsync(request.SpecId, cancellationToken)).ConfigureAwait(false);
        if (spec is null
            || spec.TenantId != request.TenantId
            || spec.ProjectId != request.ProjectId
            || !spec.IsActive)
        {
            return new SignOffSummaryDto(
                SpecId: request.SpecId.ToString(),
                TotalCount: 0,
                PendingCount: 0,
                ApprovedCount: 0,
                RejectedCount: 0,
                ConditionalCount: 0,
                SignOffs: []);
        }

        var signOffs = await (signOffRepo.FindAsync(
            s => s.SpecId == request.SpecId
                && s.TenantId == request.TenantId
                && s.ProjectId == request.ProjectId
                && s.IsActive,
            cancellationToken)).ConfigureAwait(false);

        var stakeholderIds = signOffs.Select(s => s.StakeholderId).Distinct().ToList();
        var stakeholders = await (userRepo.FindAsync(
            u => stakeholderIds.Contains(u.Id),
            cancellationToken)).ConfigureAwait(false);
        var userMap = stakeholders.ToDictionary(u => u.Id, u => new UserRefDto(u.Id.ToString(), u.DisplayName ?? u.Username, u.AvatarUrl));

        var signOffDtos = signOffs
            .OrderBy(s => s.CreatedAt.ToDateTimeOffset())
            .Select(s => SpecMapping.MapToSignOffDto(
                s,
                userMap.TryGetValue(s.StakeholderId, out var userRef) ? userRef : new UserRefDto(s.StakeholderId.ToString(), "Unknown", null)))
            .ToList();

        return new SignOffSummaryDto(
            SpecId: request.SpecId.ToString(),
            TotalCount: signOffDtos.Count,
            PendingCount: signOffDtos.Count(s => s.Decision == SignOffDecision.Pending),
            ApprovedCount: signOffDtos.Count(s => s.Decision == SignOffDecision.Approved),
            RejectedCount: signOffDtos.Count(s => s.Decision == SignOffDecision.Rejected),
            ConditionalCount: signOffDtos.Count(s => s.Decision == SignOffDecision.Conditional),
            SignOffs: signOffDtos);
    }
}

/// <summary>
/// spec get
/// </summary>
public sealed record GetSpecVersionHistoryQuery(
    GlobalUniqueId TenantId,
    GlobalUniqueId ProjectId,
    GlobalUniqueId SpecId) : IQuery<List<SpecDto>>;

public sealed class GetSpecVersionHistoryQueryHandler : IRequestHandler<GetSpecVersionHistoryQuery, List<SpecDto>>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetSpecVersionHistoryQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<List<SpecDto>> Handle(GetSpecVersionHistoryQuery request, CancellationToken cancellationToken)
    {
        var specRepo = _unitOfWork.Repository<Spec>();

        var currentSpec = await (specRepo.GetByIdAsync(request.SpecId, cancellationToken)).ConfigureAwait(false);
        if (currentSpec is null
            || currentSpec.TenantId != request.TenantId
            || currentSpec.ProjectId != request.ProjectId
            || !currentSpec.IsActive)
        {
            return [];
        }

        var allVersions = await (specRepo.FindAsync(
            s => s.TenantId == request.TenantId
                && s.ProjectId == request.ProjectId
                && s.Code == currentSpec.Code
                && s.IsActive,
            cancellationToken)).ConfigureAwait(false);

        return allVersions
            .OrderByDescending(s => s.ValidFrom.ToDateTimeOffset())
            .Select(SpecMapping.MapToDto)
            .ToList();
    }
}
