using MediatR;
using Sddp.Abstractions.DTOs;
using Sddp.Abstractions.Interfaces;
using Sddp.Application.Requests;
using Sddp.Application.Telemetry;
using Sddp.Application.Utilities;
using System.Text.Json;
using Sddp.Abstractions.Events;
using Sddp.Domain.Entities;

namespace Sddp.Application.Behaviors;

public sealed class AuditBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly IAuditLogService _auditLogService;
    private readonly IUnitOfWork _unitOfWork;

    public AuditBehavior(IAuditLogService auditLogService, IUnitOfWork unitOfWork)
    {
        _auditLogService = auditLogService;
        _unitOfWork = unitOfWork;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        using var activity = MediatRActivitySource.Source.StartActivity(
            $"Audit {typeof(TRequest).Name}");

        var response = await (next()).ConfigureAwait(false);

        if (request is IAuditableRequest<TResponse> auditable)
        {
            var audit = auditable.GetAuditLogRequest(response);
            if (audit is not null)
            {
                activity?.SetTag("audit.action", audit.Action);
                activity?.SetTag("audit.resource_type", audit.ResourceType);
                if (!string.Equals(audit.ResourceType, "auth", StringComparison.OrdinalIgnoreCase))
                {
                    var dto = new AuditLogDto(
                        Id: audit.ResourceId.ToString(),
                        Timestamp: DateTimeOffset.UtcNow.ToString("O"),
                        UserId: audit.ActorId?.ToString() ?? "",
                        UserName: "System",
                        Action: audit.Action,
                        ResourceType: audit.ResourceType,
                        ResourceId: audit.ResourceId.ToString(),
                        IpAddress: "",
                        Details: AuditPayloadDetailsMapper.Map(audit.Payload));

                    var timelineEvent = new TimelineActivityCreatedEvent(
                        audit.TenantId?.ToString() ?? "",
                        audit.ProjectId?.ToString(),
                        dto);

                    await (_unitOfWork.Repository<OutboxMessage>().AddAsync(
                        OutboxMessage.Create(
                            nameof(TimelineActivityCreatedEvent),
                            audit.ResourceType,
                            audit.ResourceId,
                            JsonSerializer.Serialize(timelineEvent, JsonOptions)),
                        cancellationToken)).ConfigureAwait(false);
                }

                await (_auditLogService.LogAsync(
                    audit.ActorId,
                    audit.Action,
                    audit.ResourceType,
                    audit.ResourceId,
                    audit.Payload,
                    audit.TenantId,
                    audit.ProjectId,
                    cancellationToken)).ConfigureAwait(false);
            }
        }

        return response;
    }
}
