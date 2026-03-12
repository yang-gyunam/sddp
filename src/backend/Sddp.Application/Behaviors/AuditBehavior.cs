using MediatR;
using Sddp.Abstractions.DTOs;
using Sddp.Abstractions.Interfaces;
using Sddp.Application.Requests;
using Sddp.Application.Telemetry;
using Sddp.Application.Utilities;

namespace Sddp.Application.Behaviors;

public sealed class AuditBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly IAuditLogService _auditLogService;
    private readonly ITimelineNotificationService? _timelineNotification;

    public AuditBehavior(
        IAuditLogService auditLogService,
        ITimelineNotificationService? timelineNotification = null)
    {
        _auditLogService = auditLogService;
        _timelineNotification = timelineNotification;
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
                await (_auditLogService.LogAsync(
                    audit.ActorId,
                    audit.Action,
                    audit.ResourceType,
                    audit.ResourceId,
                    audit.Payload,
                    audit.TenantId,
                    audit.ProjectId,
                    cancellationToken)).ConfigureAwait(false);

                // SignalR broadcast (auth)
                if (_timelineNotification is not null
                    && !string.Equals(audit.ResourceType, "auth", StringComparison.OrdinalIgnoreCase))
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

                    await (_timelineNotification.NotifyActivityCreatedAsync(
                        audit.TenantId?.ToString() ?? "",
                        audit.ProjectId?.ToString(),
                        dto,
                        cancellationToken)).ConfigureAwait(false);
                }
            }
        }

        return response;
    }
}
