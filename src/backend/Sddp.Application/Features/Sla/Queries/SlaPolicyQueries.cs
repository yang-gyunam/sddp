using System.Data;
using Dapper;
using MediatR;
using Sddp.Abstractions.DTOs;
using Sddp.Abstractions.Interfaces;
using Sddp.Abstractions.ValueObjects;
using Sddp.Application.Requests;
using Sddp.Domain.Entities;

namespace Sddp.Application.Features.Sla.Queries;

/// <summary>
/// project SLA get
/// </summary>

public sealed record GetSlaPoliciesByProjectQuery(
    GlobalUniqueId TenantId,
    GlobalUniqueId ProjectId) : IQuery<IReadOnlyList<SlaPolicyDto>>;

public sealed class GetSlaPoliciesByProjectQueryHandler
    : IRequestHandler<GetSlaPoliciesByProjectQuery, IReadOnlyList<SlaPolicyDto>>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetSlaPoliciesByProjectQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<IReadOnlyList<SlaPolicyDto>> Handle(
        GetSlaPoliciesByProjectQuery request,
        CancellationToken cancellationToken)
    {
        var repo = _unitOfWork.Repository<SlaPolicy>();
        var policies = await (repo.FindAsync(
            x => x.TenantId == request.TenantId
                && x.ProjectId == request.ProjectId
                && x.IsActive,
            cancellationToken)).ConfigureAwait(false);

        return policies.Select(SlaMapper.MapToDto).ToList();
    }
}

/// <summary>
/// SLA get (ID)
/// </summary>

public sealed record GetSlaPolicyByIdQuery(
    GlobalUniqueId TenantId,
    GlobalUniqueId ProjectId,
    GlobalUniqueId PolicyId) : IQuery<SlaPolicyDto?>;

public sealed class GetSlaPolicyByIdQueryHandler
    : IRequestHandler<GetSlaPolicyByIdQuery, SlaPolicyDto?>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetSlaPolicyByIdQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<SlaPolicyDto?> Handle(
        GetSlaPolicyByIdQuery request,
        CancellationToken cancellationToken)
    {
        var repo = _unitOfWork.Repository<SlaPolicy>();
        var policy = await (repo.GetByIdAsync(request.PolicyId, cancellationToken)).ConfigureAwait(false);

        if (policy == null
            || policy.TenantId != request.TenantId
            || policy.ProjectId != request.ProjectId
            || !policy.IsActive)
        {
            return null;
        }

        return SlaMapper.MapToDto(policy);
    }
}

/// <summary>
/// SignOff SLA status get
/// </summary>

public sealed record GetPendingSignOffSlaStatusQuery(
    GlobalUniqueId TenantId,
    GlobalUniqueId ProjectId) : IQuery<IReadOnlyList<PendingSignOffSlaDto>>;

public sealed class GetPendingSignOffSlaStatusQueryHandler
    : IRequestHandler<GetPendingSignOffSlaStatusQuery, IReadOnlyList<PendingSignOffSlaDto>>
{
    private readonly IDbConnection _connection;

    public GetPendingSignOffSlaStatusQueryHandler(IDbConnection connection)
    {
        _connection = connection;
    }

    public async Task<IReadOnlyList<PendingSignOffSlaDto>> Handle(
        GetPendingSignOffSlaStatusQuery request,
        CancellationToken cancellationToken)
    {
        var rows = await (_connection.QueryAsync<PendingSignOffRow>(
            new CommandDefinition(@"
                SELECT
                    so.id AS SignOffId,
                    s.id AS SpecId,
                    s.code AS SpecCode,
                    s.title AS SpecTitle,
                    p.display_name AS StakeholderName,
                    so.requested_at AS RequestedAt,
                    COALESCE(sp.sla_hours, 24) AS SlaHours
                FROM sign_offs so
                INNER JOIN specs s ON s.id = so.spec_id AND s.is_active = true
                INNER JOIN users u ON u.id = so.stakeholder_id
                INNER JOIN persons p ON p.id = u.person_id
                LEFT JOIN sla_policies sp
                    ON sp.tenant_id = so.tenant_id
                    AND sp.project_id = so.project_id
                    AND sp.sla_type = 'signoff'
                    AND sp.is_active = true
                WHERE so.tenant_id = @TenantId
                    AND so.project_id = @ProjectId
                    AND so.is_active = true
                    AND so.signed_at IS NULL
                ORDER BY so.requested_at ASC",
                new
                {
                    TenantId = request.TenantId.ToGuid(),
                    ProjectId = request.ProjectId.ToGuid()
                },
                cancellationToken: cancellationToken))).ConfigureAwait(false);

        var now = DateTimeOffset.UtcNow;

        return rows.Select(row => MapRowToDto(row, now)).ToList();
    }

    internal static PendingSignOffSlaDto MapRowToDto(PendingSignOffRow row, DateTimeOffset now)
    {
        var requestedAt = new DateTimeOffset(DateTime.SpecifyKind(row.RequestedAt, DateTimeKind.Utc));
        var elapsed = (now - requestedAt).TotalHours;
        var percent = (int)(elapsed / row.SlaHours * 100);

        var status = percent switch
        {
            >= 200 => "warning",
            >= 100 => "escalation",
            >= 50 => "reminder",
            _ => "on_track"
        };

        return new PendingSignOffSlaDto(
            SignOffId: row.SignOffId.ToString(),
            SpecId: row.SpecId.ToString(),
            SpecCode: row.SpecCode,
            SpecTitle: row.SpecTitle,
            StakeholderName: row.StakeholderName,
            RequestedAt: requestedAt,
            SlaHours: row.SlaHours,
            ElapsedHours: Math.Round(elapsed, 1),
            ElapsedPercent: percent,
            Status: status);
    }

    internal record PendingSignOffRow(
        Guid SignOffId,
        Guid SpecId,
        string SpecCode,
        string SpecTitle,
        string StakeholderName,
        DateTime RequestedAt,
        int SlaHours);
}
