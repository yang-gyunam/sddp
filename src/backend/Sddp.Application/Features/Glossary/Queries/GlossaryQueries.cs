using MediatR;
using Sddp.Abstractions.DTOs;
using Sddp.Abstractions.Enums;
using Sddp.Abstractions.Interfaces;
using Sddp.Abstractions.ValueObjects;
using Sddp.Application.Features.Glossary;
using Sddp.Application.Requests;
using Sddp.Application.Utilities;
using Sddp.Domain.Entities;

namespace Sddp.Application.Features.Glossary.Queries;

/// <summary>
/// glossary get (, /status)
/// </summary>
public sealed record GetGlossaryTermsQuery(
    GlobalUniqueId TenantId,
    GlobalUniqueId? ProjectId,
    int Page,
    int PageSize,
    TermCategory? Category,
    GlossaryTermStatus? Status) : IQuery<GlossaryTermPageDto>;

public sealed class GetGlossaryTermsQueryHandler : IRequestHandler<GetGlossaryTermsQuery, GlossaryTermPageDto>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetGlossaryTermsQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<GlossaryTermPageDto> Handle(GetGlossaryTermsQuery request, CancellationToken cancellationToken)
    {
        var termRepo = _unitOfWork.Repository<GlossaryTerm>();

        var (pagedTerms, totalCount) = await (termRepo.FindPagedAsync(
            t => t.TenantId == request.TenantId
                && (!request.ProjectId.HasValue || t.ProjectId == request.ProjectId.Value)
                && t.ValidTo == null
                && (request.Category == null || t.Category == request.Category)
                && (request.Status == null || t.Status == request.Status),
            request.Page, request.PageSize,
            orderBy: t => t.Term,
            cancellationToken: cancellationToken)).ConfigureAwait(false);

        var totalPages = (int)Math.Ceiling((double)totalCount / request.PageSize);
        var items = pagedTerms.Select(GlossaryMapping.MapToDto).ToList();

        return new GlossaryTermPageDto(
            Items: items,
            TotalCount: totalCount,
            Page: request.Page,
            PageSize: request.PageSize,
            TotalPages: totalPages);
    }
}

/// <summary>
/// glossary search ()
/// </summary>
public sealed record SearchGlossaryTermsQuery(
    GlobalUniqueId TenantId,
    GlobalUniqueId ProjectId,
    string Query,
    int Page,
    int PageSize,
    TermCategory? Category,
    GlossaryTermStatus? Status) : IQuery<GlossaryTermPageDto>;

public sealed class SearchGlossaryTermsQueryHandler : IRequestHandler<SearchGlossaryTermsQuery, GlossaryTermPageDto>
{
    private readonly IUnitOfWork _unitOfWork;

    public SearchGlossaryTermsQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<GlossaryTermPageDto> Handle(SearchGlossaryTermsQuery request, CancellationToken cancellationToken)
    {
        var termRepo = _unitOfWork.Repository<GlossaryTerm>();
        var lowerQuery = request.Query.ToLowerInvariant();

        var (pagedTerms, totalCount) = await (termRepo.FindPagedAsync(
            t => t.TenantId == request.TenantId
                && t.ProjectId == request.ProjectId
                && t.ValidTo == null
                && (request.Category == null || t.Category == request.Category)
                && (request.Status == null || t.Status == request.Status)
                && (t.Term.ToLower().Contains(lowerQuery)
                    || t.Definition.ToLower().Contains(lowerQuery)
                    || (t.Synonyms != null && t.Synonyms.ToLower().Contains(lowerQuery))
                    || (t.Abbreviation != null && t.Abbreviation.ToLower().Contains(lowerQuery))),
            request.Page, request.PageSize,
            orderBy: t => t.Term,
            cancellationToken: cancellationToken)).ConfigureAwait(false);

        var totalPages = (int)Math.Ceiling((double)totalCount / request.PageSize);
        var items = pagedTerms.Select(GlossaryMapping.MapToDto).ToList();

        return new GlossaryTermPageDto(
            Items: items,
            TotalCount: totalCount,
            Page: request.Page,
            PageSize: request.PageSize,
            TotalPages: totalPages);
    }
}

/// <summary>
/// glossary ()
/// </summary>
public sealed record AutocompleteGlossaryTermsQuery(
    GlobalUniqueId TenantId,
    GlobalUniqueId ProjectId,
    string Prefix,
    int Limit) : IQuery<List<GlossaryTermSummaryDto>>;

public sealed class AutocompleteGlossaryTermsQueryHandler : IRequestHandler<AutocompleteGlossaryTermsQuery, List<GlossaryTermSummaryDto>>
{
    private readonly IUnitOfWork _unitOfWork;

    public AutocompleteGlossaryTermsQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<List<GlossaryTermSummaryDto>> Handle(AutocompleteGlossaryTermsQuery request, CancellationToken cancellationToken)
    {
        var termRepo = _unitOfWork.Repository<GlossaryTerm>();
        var lowerPrefix = request.Prefix.ToLowerInvariant();

        var terms = await (termRepo.FindAsync(
            t => t.TenantId == request.TenantId
                && t.ProjectId == request.ProjectId
                && t.IsActive
                && t.ValidTo == null
                && t.Status == GlossaryTermStatus.Active
                && t.Term.ToLower().StartsWith(lowerPrefix),
            cancellationToken)).ConfigureAwait(false);

        return terms
            .OrderBy(t => t.Term)
            .Take(request.Limit)
            .Select(GlossaryMapping.MapToSummaryDto)
            .ToList();
    }
}

/// <summary>
/// glossary get (ID)
/// </summary>
public sealed record GetGlossaryTermByIdQuery(
    GlobalUniqueId TenantId,
    GlobalUniqueId ProjectId,
    GlobalUniqueId TermId) : IQuery<GlossaryTermDetailDto?>;

public sealed class GetGlossaryTermByIdQueryHandler : IRequestHandler<GetGlossaryTermByIdQuery, GlossaryTermDetailDto?>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetGlossaryTermByIdQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<GlossaryTermDetailDto?> Handle(GetGlossaryTermByIdQuery request, CancellationToken cancellationToken)
    {
        var termRepo = _unitOfWork.Repository<GlossaryTerm>();

        var term = await (termRepo.GetByIdAsync(request.TermId, cancellationToken)).ConfigureAwait(false);
        if (term is null
            || term.TenantId != request.TenantId
            || term.ProjectId != request.ProjectId
            || !term.IsActive)
        {
            return null;
        }

        return await (GlossaryMapping.MapToDetailDtoAsync(term, _unitOfWork, cancellationToken)).ConfigureAwait(false);
    }
}

/// <summary>
/// glossary get (glossary)
/// </summary>
public sealed record GetGlossaryTermByTermQuery(
    GlobalUniqueId TenantId,
    GlobalUniqueId ProjectId,
    string Term) : IQuery<GlossaryTermDetailDto?>;

public sealed class GetGlossaryTermByTermQueryHandler : IRequestHandler<GetGlossaryTermByTermQuery, GlossaryTermDetailDto?>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetGlossaryTermByTermQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<GlossaryTermDetailDto?> Handle(GetGlossaryTermByTermQuery request, CancellationToken cancellationToken)
    {
        var termRepo = _unitOfWork.Repository<GlossaryTerm>();

        var terms = await (termRepo.FindAsync(
            t => t.TenantId == request.TenantId
                && t.ProjectId == request.ProjectId
                && t.Term == request.Term
                && t.IsActive
                && t.ValidTo == null,
            cancellationToken)).ConfigureAwait(false);

        var foundTerm = terms.FirstOrDefault();
        if (foundTerm is null)
        {
            return null;
        }

        return await (GlossaryMapping.MapToDetailDtoAsync(foundTerm, _unitOfWork, cancellationToken)).ConfigureAwait(false);
    }
}

/// <summary>
/// glossary (glossary,,)
/// </summary>
public sealed record DetectGlossaryConflictsQuery(
    GlobalUniqueId TenantId,
    GlobalUniqueId ProjectId,
    string Term,
    string? Definition,
    GlobalUniqueId? ExcludeTermId) : IQuery<GlossaryConflictResultDto>;

public sealed class DetectGlossaryConflictsQueryHandler : IRequestHandler<DetectGlossaryConflictsQuery, GlossaryConflictResultDto>
{
    private readonly IUnitOfWork _unitOfWork;

    public DetectGlossaryConflictsQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<GlossaryConflictResultDto> Handle(DetectGlossaryConflictsQuery request, CancellationToken cancellationToken)
    {
        var termRepo = _unitOfWork.Repository<GlossaryTerm>();
        var conflicts = new List<GlossaryConflictDto>();
        var lowerTerm = request.Term.ToLowerInvariant();

        var sameTerms = await (termRepo.FindAsync(
            t => t.TenantId == request.TenantId
                && t.ProjectId == request.ProjectId
                && t.Term.ToLower() == lowerTerm
                && t.IsActive
                && t.ValidTo == null
                && (request.ExcludeTermId == null || t.Id != request.ExcludeTermId),
            cancellationToken)).ConfigureAwait(false);

        foreach (var term in sameTerms)
        {
            conflicts.Add(new GlossaryConflictDto(
                TermId: term.Id.ToString(),
                Term: term.Term,
                ConflictType: "SameTerm",
                Message: $"Term '{term.Term}' already exists"));
        }

        var synonymConflicts = await (termRepo.FindAsync(
            t => t.TenantId == request.TenantId
                && t.ProjectId == request.ProjectId
                && t.IsActive
                && t.ValidTo == null
                && t.Synonyms != null
                && t.Synonyms.ToLower().Contains(lowerTerm)
                && (request.ExcludeTermId == null || t.Id != request.ExcludeTermId),
            cancellationToken)).ConfigureAwait(false);

        foreach (var term in synonymConflicts)
        {
            conflicts.Add(new GlossaryConflictDto(
                TermId: term.Id.ToString(),
                Term: term.Term,
                ConflictType: "Synonym",
                Message: $"Term '{request.Term}' is listed as a synonym for '{term.Term}'"));
        }

        var abbreviationConflicts = await (termRepo.FindAsync(
            t => t.TenantId == request.TenantId
                && t.ProjectId == request.ProjectId
                && t.IsActive
                && t.ValidTo == null
                && t.Abbreviation != null
                && t.Abbreviation.ToLower() == lowerTerm
                && (request.ExcludeTermId == null || t.Id != request.ExcludeTermId),
            cancellationToken)).ConfigureAwait(false);

        foreach (var term in abbreviationConflicts)
        {
            conflicts.Add(new GlossaryConflictDto(
                TermId: term.Id.ToString(),
                Term: term.Term,
                ConflictType: "Abbreviation",
                Message: $"Term '{request.Term}' is the abbreviation for '{term.Term}'"));
        }

        return new GlossaryConflictResultDto(
            HasConflict: conflicts.Count > 0,
            Conflicts: conflicts);
    }
}

/// <summary>
/// glossary get (Spec search)
/// </summary>
public sealed record GetGlossaryTermUsageQuery(
    GlobalUniqueId TenantId,
    GlobalUniqueId ProjectId,
    GlobalUniqueId TermId) : IQuery<GlossaryTermUsageDto>;

public sealed class GetGlossaryTermUsageQueryHandler : IRequestHandler<GetGlossaryTermUsageQuery, GlossaryTermUsageDto>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetGlossaryTermUsageQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<GlossaryTermUsageDto> Handle(GetGlossaryTermUsageQuery request, CancellationToken cancellationToken)
    {
        var termRepo = _unitOfWork.Repository<GlossaryTerm>();
        var specRepo = _unitOfWork.Repository<Spec>();

        var term = await (termRepo.GetByIdAsync(request.TermId, cancellationToken)).ConfigureAwait(false);
        if (term is null || !term.IsActive)
        {
            return new GlossaryTermUsageDto(
                TermId: request.TermId.ToString(),
                Term: string.Empty,
                UsageCount: 0,
                Usages: []);
        }

        var usages = new List<GlossaryTermUsageItemDto>();
        var termName = term.Term.ToLowerInvariant();

        var specs = await (specRepo.FindAsync(
            s => s.TenantId == request.TenantId
                && s.ProjectId == request.ProjectId
                && s.IsActive
                && s.ValidTo == null
                && ((s.Description ?? string.Empty).ToLower().Contains(termName)
                    || (s.Decision ?? string.Empty).ToLower().Contains(termName)
                    || (s.Context ?? string.Empty).ToLower().Contains(termName)),
            cancellationToken)).ConfigureAwait(false);

        foreach (var spec in specs)
        {
            var fields = new List<string>();
            if ((spec.Description ?? string.Empty).ToLower().Contains(termName)) fields.Add("Description");
            if ((spec.Decision ?? string.Empty).ToLower().Contains(termName)) fields.Add("Decision");
            if ((spec.Context ?? string.Empty).ToLower().Contains(termName)) fields.Add("Context");

            usages.Add(new GlossaryTermUsageItemDto(
                EntityType: "Spec",
                EntityId: spec.Id.ToString(),
                EntityTitle: spec.Title,
                FieldName: string.Join(", ", fields)));
        }

        return new GlossaryTermUsageDto(
            TermId: request.TermId.ToString(),
            Term: term.Term,
            UsageCount: usages.Count,
            Usages: usages);
    }
}

/// <summary>
/// glossary get
/// </summary>
public sealed record GetGlossaryTermVersionHistoryQuery(
    GlobalUniqueId TenantId,
    GlobalUniqueId ProjectId,
    GlobalUniqueId TermId) : IQuery<List<GlossaryTermVersionDto>>;

public sealed class GetGlossaryTermVersionHistoryQueryHandler : IRequestHandler<GetGlossaryTermVersionHistoryQuery, List<GlossaryTermVersionDto>>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetGlossaryTermVersionHistoryQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<List<GlossaryTermVersionDto>> Handle(GetGlossaryTermVersionHistoryQuery request, CancellationToken cancellationToken)
    {
        var termRepo = _unitOfWork.Repository<GlossaryTerm>();

        var currentTerm = await (termRepo.GetByIdAsync(request.TermId, cancellationToken)).ConfigureAwait(false);
        if (currentTerm is null
            || currentTerm.TenantId != request.TenantId
            || currentTerm.ProjectId != request.ProjectId
            || !currentTerm.IsActive)
        {
            return [];
        }

        var allVersions = await (termRepo.FindAsync(
            t => t.TenantId == request.TenantId
                && t.ProjectId == request.ProjectId
                && t.Term == currentTerm.Term
                && t.IsActive,
            cancellationToken)).ConfigureAwait(false);

        var userRepo = _unitOfWork.Repository<User>();
        var userIds = allVersions
            .SelectMany(t => new[] { t.DefinedBy, t.UpdatedBy })
            .Distinct()
            .ToList();

        var userRefs = new Dictionary<GlobalUniqueId, UserRefDto>();
        foreach (var userId in userIds)
        {
            var userRef = await (UserRefHelper.ToUserRefAsync(userRepo, userId, cancellationToken)).ConfigureAwait(false);
            userRefs[userId] = userRef;
        }

        return allVersions
            .OrderByDescending(t => t.ValidFrom.ToDateTimeOffset())
            .Select(t => GlossaryMapping.MapToVersionDto(
                t,
                userRefs.GetValueOrDefault(t.DefinedBy),
                userRefs.GetValueOrDefault(t.UpdatedBy)))
            .ToList();
    }
}
