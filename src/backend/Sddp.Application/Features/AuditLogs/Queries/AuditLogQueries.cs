using MediatR;
using Sddp.Abstractions.DTOs;
using Sddp.Abstractions.Interfaces;
using Sddp.Abstractions.ValueObjects;
using Sddp.Application.Features.AuditLogs;
using Sddp.Application.Requests;
using Sddp.Domain.Entities;

namespace Sddp.Application.Features.AuditLogs.Queries;

/// <summary>
/// audit log get (+)
/// </summary>
public sealed record GetAuditLogsQuery(
    GlobalUniqueId? TenantId,
    GlobalUniqueId? ProjectId,
    GlobalUniqueId? ActorId,
    string? Action,
    string? ResourceType,
    IReadOnlyList<string>? ExcludedResourceTypes,
    DateTimeOffset? StartDate,
    DateTimeOffset? EndDate,
    int PageNumber,
    int PageSize) : IQuery<PagedResult<AuditLogDto>>;

public sealed class GetAuditLogsQueryHandler : IRequestHandler<GetAuditLogsQuery, PagedResult<AuditLogDto>>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetAuditLogsQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<PagedResult<AuditLogDto>> Handle(GetAuditLogsQuery request, CancellationToken cancellationToken)
    {
        var auditRepo = _unitOfWork.Repository<AuditLog>();
        var userRepo = _unitOfWork.Repository<User>();

        var action = request.Action;
        var hasAction = !string.IsNullOrWhiteSpace(action);
        var resourceType = request.ResourceType;
        var hasResourceType = !string.IsNullOrWhiteSpace(resourceType);
        var excludedTypes = request.ExcludedResourceTypes?.ToList() ?? [];
        var hasExclusions = excludedTypes.Count > 0;

        var (pagedLogs, totalCount) = await (auditRepo.FindPagedAsync(
            l => (!request.TenantId.HasValue || l.TenantId == request.TenantId)
                && (!request.ProjectId.HasValue || l.ProjectId == request.ProjectId)
                && (!request.ActorId.HasValue || l.ActorId == request.ActorId)
                && (!hasAction || l.Action == action!)
                && (!hasResourceType || l.ResourceType == resourceType!)
                && (!hasExclusions || !excludedTypes.Contains(l.ResourceType)),
            request.PageNumber, request.PageSize,
            q => q.OrderByDescending(l => l.CreatedAt),
            cancellationToken)).ConfigureAwait(false);

        // : Timestamp.ToDateTimeOffset() EF Core SQL → in-memory
        // DB
        IEnumerable<AuditLog> filtered = pagedLogs;
        if (request.StartDate.HasValue)
            filtered = filtered.Where(l => l.CreatedAt.ToDateTimeOffset() >= request.StartDate.Value);
        if (request.EndDate.HasValue)
            filtered = filtered.Where(l => l.CreatedAt.ToDateTimeOffset() <= request.EndDate.Value);

        var actorIds = filtered.Where(l => l.ActorId.HasValue).Select(l => l.ActorId!.Value).Distinct().ToList();
        var actors = actorIds.Count == 0
            ? Array.Empty<User>()
            : await (userRepo.FindAsync(u => actorIds.Contains(u.Id), cancellationToken)).ConfigureAwait(false);
        var userMap = actors.ToDictionary(u => u.Id, u => u.DisplayName);

        var items = filtered
            .Select(l => AuditLogHelpers.MapToDto(l, userMap))
            .ToList();

        return PagedResult<AuditLogDto>.Create(items, totalCount, request.PageNumber, request.PageSize);
    }
}

/// <summary>
/// audit log get (ID)
/// </summary>
public sealed record GetAuditLogByIdQuery(GlobalUniqueId LogId) : IQuery<AuditLogDto?>;

public sealed class GetAuditLogByIdQueryHandler : IRequestHandler<GetAuditLogByIdQuery, AuditLogDto?>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetAuditLogByIdQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<AuditLogDto?> Handle(GetAuditLogByIdQuery request, CancellationToken cancellationToken)
    {
        var auditRepo = _unitOfWork.Repository<AuditLog>();
        var log = await (auditRepo.GetByIdAsync(request.LogId, cancellationToken)).ConfigureAwait(false);

        if (log is null)
        {
            return null;
        }

        var userMap = new Dictionary<GlobalUniqueId, string>();
        if (log.ActorId.HasValue)
        {
            var userRepo = _unitOfWork.Repository<User>();
            var actor = await (userRepo.GetByIdAsync(log.ActorId.Value, cancellationToken)).ConfigureAwait(false);
            if (actor is not null)
            {
                userMap[actor.Id] = actor.DisplayName;
            }
        }

        return AuditLogHelpers.MapToDto(log, userMap);
    }
}

/// <summary>
/// audit log get
/// </summary>
public sealed record GetAuditLogsByResourceQuery(
    string ResourceType,
    GlobalUniqueId ResourceId,
    int Limit = 100) : IQuery<List<AuditLogDto>>;

public sealed class GetAuditLogsByResourceQueryHandler : IRequestHandler<GetAuditLogsByResourceQuery, List<AuditLogDto>>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetAuditLogsByResourceQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<List<AuditLogDto>> Handle(GetAuditLogsByResourceQuery request, CancellationToken cancellationToken)
    {
        var auditRepo = _unitOfWork.Repository<AuditLog>();
        var userRepo = _unitOfWork.Repository<User>();

        var logs = await (auditRepo.FindAsync(
            l => l.IsActive
                && l.ResourceType == request.ResourceType
                && l.ResourceId == request.ResourceId,
            cancellationToken)).ConfigureAwait(false);

        var ordered = logs
            .OrderByDescending(l => l.CreatedAt)
            .Take(request.Limit)
            .ToList();

        var allUsers = await (userRepo.GetAllAsync(cancellationToken)).ConfigureAwait(false);
        var userMap = allUsers.ToDictionary(u => u.Id, u => u.DisplayName);

        return ordered.Select(l => AuditLogHelpers.MapToDto(l, userMap)).ToList();
    }
}

/// <summary>
/// get
/// </summary>
public sealed record GetFieldAuthorsQuery(
    string ResourceType,
    GlobalUniqueId ResourceId) : IQuery<List<FieldAuthorDto>>;

public sealed class GetFieldAuthorsQueryHandler : IRequestHandler<GetFieldAuthorsQuery, List<FieldAuthorDto>>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetFieldAuthorsQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<List<FieldAuthorDto>> Handle(GetFieldAuthorsQuery request, CancellationToken cancellationToken)
    {
        var auditRepo = _unitOfWork.Repository<AuditLog>();
        var userRepo = _unitOfWork.Repository<User>();

        var logs = await (auditRepo.FindAsync(
            l => l.IsActive
                && l.ResourceType == request.ResourceType
                && l.ResourceId == request.ResourceId
                && (l.Action == "update" || l.Action == "create"),
            cancellationToken)).ConfigureAwait(false);

        var ordered = logs.OrderByDescending(l => l.CreatedAt).ToList();

        var allUsers = await (userRepo.GetAllAsync(cancellationToken)).ConfigureAwait(false);
        var userMap = allUsers.ToDictionary(u => u.Id, u => u.DisplayName);

        return AuditLogHelpers.BuildFieldAuthors(ordered, userMap, request.ResourceType);
    }
}

/// <summary>
/// get
/// </summary>
public sealed record GetTimelineByResourceQuery(
    string ResourceType,
    GlobalUniqueId ResourceId) : IQuery<IReadOnlyList<AuditLogEntry>>;

public sealed class GetTimelineByResourceQueryHandler : IRequestHandler<GetTimelineByResourceQuery, IReadOnlyList<AuditLogEntry>>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetTimelineByResourceQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<IReadOnlyList<AuditLogEntry>> Handle(GetTimelineByResourceQuery request, CancellationToken cancellationToken)
    {
        var repo = _unitOfWork.Repository<AuditLog>();

        var logs = await (repo.FindAsync(
            l => l.IsActive
                && l.ResourceType == request.ResourceType
                && l.ResourceId == request.ResourceId,
            cancellationToken)).ConfigureAwait(false);

        return logs
            .OrderByDescending(l => l.CreatedAt)
            .Select(AuditLogEntryMapper.MapToEntry)
            .ToList()
            .AsReadOnly();
    }
}

/// <summary>
/// get
/// </summary>
public sealed record GetTimelineByActorQuery(GlobalUniqueId ActorId) : IQuery<IReadOnlyList<AuditLogEntry>>;

public sealed class GetTimelineByActorQueryHandler : IRequestHandler<GetTimelineByActorQuery, IReadOnlyList<AuditLogEntry>>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetTimelineByActorQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<IReadOnlyList<AuditLogEntry>> Handle(GetTimelineByActorQuery request, CancellationToken cancellationToken)
    {
        var repo = _unitOfWork.Repository<AuditLog>();

        var logs = await (repo.FindAsync(
            l => l.IsActive && l.ActorId == request.ActorId,
            cancellationToken)).ConfigureAwait(false);

        return logs
            .OrderByDescending(l => l.CreatedAt)
            .Select(AuditLogEntryMapper.MapToEntry)
            .ToList()
            .AsReadOnly();
    }
}

internal static class AuditLogEntryMapper
{
    internal static AuditLogEntry MapToEntry(AuditLog log)
    {
        return new AuditLogEntry(
            Id: log.Id,
            ActorId: log.ActorId,
            Action: log.Action,
            ResourceType: log.ResourceType,
            ResourceId: log.ResourceId,
            Payload: log.Payload,
            CreatedAt: log.CreatedAt);
    }
}
