using MediatR;
using Microsoft.Extensions.Logging;
using Sddp.Abstractions.DTOs;
using Sddp.Abstractions.Interfaces;
using Sddp.Abstractions.ValueObjects;
using Sddp.Application.Features.Artifacts;
using Sddp.Application.Requests;
using Sddp.Domain.Entities;
using GlossaryTerm = Sddp.Domain.Entities.GlossaryTerm;
using User = Sddp.Domain.Entities.User;

namespace Sddp.Application.Features.Artifacts.Queries;

/// <summary>
/// project all get
/// </summary>
public sealed record GetProjectArtifactsQuery(
    GlobalUniqueId TenantId,
    GlobalUniqueId ProjectId) : IQuery<IReadOnlyList<ArtifactTrackingSummaryDto>>;

public sealed class GetProjectArtifactsQueryHandler : IRequestHandler<GetProjectArtifactsQuery, IReadOnlyList<ArtifactTrackingSummaryDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IArtifactRepositoryService _artifactRepositoryService;
    private readonly ILogger<GetProjectArtifactsQueryHandler> _logger;

    public GetProjectArtifactsQueryHandler(
        IUnitOfWork unitOfWork,
        IArtifactRepositoryService artifactRepositoryService,
        ILogger<GetProjectArtifactsQueryHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _artifactRepositoryService = artifactRepositoryService;
        _logger = logger;
    }

    public async Task<IReadOnlyList<ArtifactTrackingSummaryDto>> Handle(
        GetProjectArtifactsQuery request,
        CancellationToken cancellationToken)
    {
        var artifactRepo = _unitOfWork.Repository<ArtifactTracking>();
        var specRepo = _unitOfWork.Repository<Spec>();

        var artifacts = await (artifactRepo.FindAsync(
            x => x.TenantId == request.TenantId
                && x.ProjectId == request.ProjectId
                && x.IsActive,
            cancellationToken)).ConfigureAwait(false);

        var specIds = artifacts.Select(a => a.SpecId).Distinct().ToList();
        var specs = await (specRepo.FindAsync(
            s => specIds.Contains(s.Id) && s.IsActive,
            cancellationToken)).ConfigureAwait(false);
        var specCodeMap = specs.ToDictionary(s => s.Id, s => s.Code);
        var specTitleMap = specs.ToDictionary(s => s.Id, s => s.Title);

        // Build glossary term name map
        var glossaryTermIds = artifacts
            .Where(a => a.GlossaryTermId.HasValue)
            .Select(a => a.GlossaryTermId!.Value)
            .Distinct()
            .ToList();
        Dictionary<GlobalUniqueId, string>? glossaryTermNameMap = null;
        if (glossaryTermIds.Count > 0)
        {
            var glossaryRepo = _unitOfWork.Repository<GlossaryTerm>();
            var terms = await (glossaryRepo.FindAsync(
                t => glossaryTermIds.Contains(t.Id) && t.IsActive,
                cancellationToken)).ConfigureAwait(false);
            glossaryTermNameMap = terms.ToDictionary(t => t.Id, t => t.Term);
        }

        // Build conversation name map
        var conversationIds = artifacts
            .Where(a => a.SourceConversationId.HasValue)
            .Select(a => a.SourceConversationId!.Value)
            .Distinct()
            .ToList();
        Dictionary<GlobalUniqueId, string>? conversationNameMap = null;
        if (conversationIds.Count > 0)
        {
            var conversationRepo = _unitOfWork.Repository<Conversation>();
            var conversations = await (conversationRepo.FindAsync(
                c => conversationIds.Contains(c.Id) && c.IsActive,
                cancellationToken)).ConfigureAwait(false);
            conversationNameMap = conversations.ToDictionary(c => c.Id, c => c.Name);
        }

        // Build requirement code map
        var requirementIds = artifacts
            .Where(a => a.SourceRequirementId.HasValue)
            .Select(a => a.SourceRequirementId!.Value)
            .Distinct()
            .ToList();
        Dictionary<GlobalUniqueId, string>? requirementCodeMap = null;
        if (requirementIds.Count > 0)
        {
            var requirementRepo = _unitOfWork.Repository<Requirement>();
            var requirements = await (requirementRepo.FindAsync(
                r => requirementIds.Contains(r.Id) && r.IsActive,
                cancellationToken)).ConfigureAwait(false);
            requirementCodeMap = requirements.ToDictionary(r => r.Id, r => r.Code);
        }

        // Build user ref map for created_by / updated_by / owner_user_id
        var userIds = artifacts
            .SelectMany(a => new GlobalUniqueId[] { a.CreatedBy, a.UpdatedBy }
                .Concat(a.OwnerUserId.HasValue ? new[] { a.OwnerUserId.Value } : Array.Empty<GlobalUniqueId>()))
            .Distinct()
            .ToList();
        Dictionary<GlobalUniqueId, UserRefDto>? userRefMap = null;
        if (userIds.Count > 0)
        {
            var userRepo = _unitOfWork.Repository<User>();
            var users = await (userRepo.FindAsync(
                u => userIds.Contains(u.Id),
                cancellationToken)).ConfigureAwait(false);
            userRefMap = users.ToDictionary(u => u.Id, u => new UserRefDto(u.Id.ToString(), u.DisplayName, u.AvatarUrl));
        }

        // Try to sync the git repository, but don't fail if repo is unreachable
        ArtifactRepositoryContext? repoContext = null;
        var repoSynced = false;
        try
        {
            repoContext = await (_artifactRepositoryService.GetProjectContextAsync(
                request.TenantId,
                request.ProjectId,
                cancellationToken)).ConfigureAwait(false);
            await (_artifactRepositoryService.EnsureProjectSyncedAsync(repoContext, cancellationToken)).ConfigureAwait(false);
            repoSynced = true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to sync git repository for project {ProjectId}. Artifact file status will be unavailable.", request.ProjectId);
        }

        var summaries = new List<ArtifactTrackingSummaryDto>(artifacts.Count());
        foreach (var artifact in artifacts)
        {
            string status;
            if (repoSynced && repoContext is not null)
            {
                status = await (ArtifactStatusHelper.DetermineStatusAsync(
                    _artifactRepositoryService,
                    repoContext,
                    artifact,
                    cancellationToken)).ConfigureAwait(false);
            }
            else
            {
                status = "Unverified";
            }
            summaries.Add(ArtifactMapping.MapToSummaryDto(artifact, specCodeMap, specTitleMap, glossaryTermNameMap, conversationNameMap, requirementCodeMap, userRefMap, status));
        }

        return summaries;
    }
}

/// <summary>
/// spec get
/// </summary>
public sealed record GetArtifactsBySpecQuery(
    GlobalUniqueId TenantId,
    GlobalUniqueId ProjectId,
    GlobalUniqueId SpecId) : IQuery<IReadOnlyList<ArtifactTrackingDto>>;

public sealed class GetArtifactsBySpecQueryHandler : IRequestHandler<GetArtifactsBySpecQuery, IReadOnlyList<ArtifactTrackingDto>>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetArtifactsBySpecQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<IReadOnlyList<ArtifactTrackingDto>> Handle(
        GetArtifactsBySpecQuery request,
        CancellationToken cancellationToken)
    {
        var repo = _unitOfWork.Repository<ArtifactTracking>();
        var artifacts = await (repo.FindAsync(
            x => x.TenantId == request.TenantId
                && x.ProjectId == request.ProjectId
                && x.SpecId == request.SpecId
                && x.IsActive,
            cancellationToken)).ConfigureAwait(false);

        return artifacts.Select(ArtifactMapping.MapToDto).ToList();
    }
}

/// <summary>
/// get (ID)
/// </summary>
public sealed record GetArtifactByIdQuery(
    GlobalUniqueId TenantId,
    GlobalUniqueId ProjectId,
    GlobalUniqueId ArtifactId) : IQuery<ArtifactTrackingDto?>;

public sealed class GetArtifactByIdQueryHandler : IRequestHandler<GetArtifactByIdQuery, ArtifactTrackingDto?>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetArtifactByIdQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<ArtifactTrackingDto?> Handle(GetArtifactByIdQuery request, CancellationToken cancellationToken)
    {
        var repo = _unitOfWork.Repository<ArtifactTracking>();
        var artifact = await (repo.GetByIdAsync(request.ArtifactId, cancellationToken)).ConfigureAwait(false);

        if (artifact == null
            || artifact.TenantId != request.TenantId
            || artifact.ProjectId != request.ProjectId
            || !artifact.IsActive)
        {
            return null;
        }

        return ArtifactMapping.MapToDto(artifact);
    }
}

/// <summary>
/// get ()
/// </summary>
public sealed record GetArtifactByPathQuery(
    GlobalUniqueId TenantId,
    GlobalUniqueId ProjectId,
    string Path) : IQuery<ArtifactTrackingDto?>;

public sealed class GetArtifactByPathQueryHandler : IRequestHandler<GetArtifactByPathQuery, ArtifactTrackingDto?>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetArtifactByPathQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<ArtifactTrackingDto?> Handle(GetArtifactByPathQuery request, CancellationToken cancellationToken)
    {
        var repo = _unitOfWork.Repository<ArtifactTracking>();
        var artifacts = await (repo.FindAsync(
            x => x.TenantId == request.TenantId
                && x.ProjectId == request.ProjectId
                && x.ArtifactPath == request.Path
                && x.IsActive,
            cancellationToken)).ConfigureAwait(false);

        var artifact = artifacts.FirstOrDefault();
        return artifact == null ? null : ArtifactMapping.MapToDto(artifact);
    }
}

/// <summary>
/// search (/entity)
/// </summary>
public sealed record SearchArtifactsQuery(
    GlobalUniqueId TenantId,
    GlobalUniqueId ProjectId,
    string Query,
    int Limit = 15) : IQuery<IEnumerable<ArtifactSearchResultDto>>;

public sealed class SearchArtifactsQueryHandler : IRequestHandler<SearchArtifactsQuery, IEnumerable<ArtifactSearchResultDto>>
{
    private readonly IUnitOfWork _unitOfWork;

    public SearchArtifactsQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<IEnumerable<ArtifactSearchResultDto>> Handle(SearchArtifactsQuery request, CancellationToken cancellationToken)
    {
        var repo = _unitOfWork.Repository<ArtifactTracking>();
        var queryLower = request.Query.ToLower();

        var matches = await (repo.FindAsync(
            a => a.TenantId == request.TenantId
                && a.ProjectId == request.ProjectId
                && a.IsActive
                && (a.ArtifactPath.ToLower().Contains(queryLower)
                    || a.EntityName.ToLower().Contains(queryLower)),
            cancellationToken)).ConfigureAwait(false);

        return matches
            .OrderBy(a => a.ArtifactPath)
            .Take(request.Limit)
            .Select(ArtifactMapping.MapToSearchResultDto)
            .ToList();
    }
}
