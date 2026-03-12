using MediatR;
using Sddp.Abstractions.DTOs;
using Sddp.Abstractions.Interfaces;
using Sddp.Abstractions.ValueObjects;
using Sddp.Application.Requests;

namespace Sddp.Application.Features.Dashboard.Queries;

/// <summary>
/// personal dashboard get
/// </summary>
public sealed record GetMyDashboardWidgetsQuery(
    GlobalUniqueId TenantId,
    GlobalUniqueId UserId) : IQuery<MyDashboardWidgetsDto>;

public sealed class GetMyDashboardWidgetsQueryHandler : IRequestHandler<GetMyDashboardWidgetsQuery, MyDashboardWidgetsDto>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAuditLogService _auditLogService;

    public GetMyDashboardWidgetsQueryHandler(IUnitOfWork unitOfWork, IAuditLogService auditLogService)
    {
        _unitOfWork = unitOfWork;
        _auditLogService = auditLogService;
    }

    public async Task<MyDashboardWidgetsDto> Handle(GetMyDashboardWidgetsQuery request, CancellationToken cancellationToken)
    {
        var specHealth = await (DashboardWidgetHelpers.BuildSpecHealthAsync(_unitOfWork, request.TenantId, request.UserId, cancellationToken)).ConfigureAwait(false);
        var signOffQueue = await (DashboardWidgetHelpers.BuildSignOffQueueAsync(_unitOfWork, request.TenantId, request.UserId, cancellationToken)).ConfigureAwait(false);
        var heatmap = await (DashboardWidgetHelpers.BuildContributionHeatmapAsync(_unitOfWork, request.UserId, cancellationToken)).ConfigureAwait(false);
        var spotlight = await (DashboardWidgetHelpers.BuildProjectSpotlightAsync(_unitOfWork, request.TenantId, request.UserId, cancellationToken)).ConfigureAwait(false);
        var timeline = await (DashboardWidgetHelpers.BuildDueDateTimelineAsync(_unitOfWork, request.TenantId, request.UserId, cancellationToken)).ConfigureAwait(false);
        var effort = await (DashboardWidgetHelpers.BuildEffortTrackerAsync(_unitOfWork, request.TenantId, request.UserId, cancellationToken)).ConfigureAwait(false);

        return new MyDashboardWidgetsDto(specHealth, signOffQueue, heatmap, spotlight, timeline, effort);
    }
}
