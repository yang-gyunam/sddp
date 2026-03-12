using System.Text.Json;
using Sddp.Abstractions.DTOs;
using Sddp.Abstractions.Interfaces;
using Sddp.Abstractions.ValueObjects;
using Sddp.Domain.Entities;

namespace Sddp.Application.Services;

/// <summary>
/// Audit log service implementation.
/// </summary>
public class AuditLogService : IAuditLogService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IRequestContextProvider? _requestContext;

    public AuditLogService(IUnitOfWork unitOfWork, IRequestContextProvider? requestContext = null)
    {
        _unitOfWork = unitOfWork;
        _requestContext = requestContext;
    }

    public Task LogAsync(
        GlobalUniqueId? actorId,
        string action,
        string resourceType,
        GlobalUniqueId resourceId,
        object? payload = null,
        CancellationToken cancellationToken = default)
    {
        return LogAsync(actorId, action, resourceType, resourceId, payload,
            tenantId: null, projectId: null, cancellationToken);
    }

    public async Task LogAsync(
        GlobalUniqueId? actorId,
        string action,
        string resourceType,
        GlobalUniqueId resourceId,
        object? payload,
        GlobalUniqueId? tenantId,
        GlobalUniqueId? projectId,
        CancellationToken cancellationToken = default)
    {
        var payloadJson = payload is not null
            ? JsonSerializer.Serialize(payload)
            : null;

        // Auto-enrich from request context
        var ipAddress = _requestContext?.IpAddress;
        var userAgent = _requestContext?.UserAgent;
        var correlationId = _requestContext?.CorrelationId;

        var auditLog = AuditLog.Create(
            actorId,
            action,
            resourceType,
            resourceId,
            payloadJson,
            ipAddress,
            userAgent,
            correlationId,
            tenantId,
            projectId);

        var repo = _unitOfWork.Repository<AuditLog>();
        await (repo.AddAsync(auditLog, cancellationToken)).ConfigureAwait(false);
        await (_unitOfWork.SaveChangesAsync(cancellationToken)).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<AuditLogEntry>> GetByResourceAsync(
        string resourceType,
        GlobalUniqueId resourceId,
        CancellationToken cancellationToken = default)
    {
        var repo = _unitOfWork.Repository<AuditLog>();

        var logs = await (repo.FindAsync(
            l => l.IsActive
                && l.ResourceType == resourceType
                && l.ResourceId == resourceId,
            cancellationToken)).ConfigureAwait(false);

        return logs
            .OrderByDescending(l => l.CreatedAt)
            .Select(MapToEntry)
            .ToList()
            .AsReadOnly();
    }

    public async Task<IReadOnlyList<AuditLogEntry>> GetByActorAsync(
        GlobalUniqueId actorId,
        CancellationToken cancellationToken = default)
    {
        var repo = _unitOfWork.Repository<AuditLog>();

        var logs = await (repo.FindAsync(
            l => l.IsActive && l.ActorId == actorId,
            cancellationToken)).ConfigureAwait(false);

        return logs
            .OrderByDescending(l => l.CreatedAt)
            .Select(MapToEntry)
            .ToList()
            .AsReadOnly();
    }

    private static AuditLogEntry MapToEntry(AuditLog log)
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
