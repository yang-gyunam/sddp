using Sddp.Abstractions.DTOs;
using Sddp.Abstractions.ValueObjects;
using Sddp.Application.Requests;

namespace Sddp.Application.Features.Diagrams;

public sealed record CreateDiagramCommand(GlobalUniqueId TenantId, GlobalUniqueId ProjectId,
    GlobalUniqueId UserId, CreateDiagramDto Dto) : ICommand<DiagramDto>, IAuditableRequest<DiagramDto>
{
    public AuditLogRequest? AuditLog { get; set; }
    public AuditLogRequest? GetAuditLogRequest(DiagramDto response) => AuditLog;
}

public sealed record UpdateDiagramCommand(GlobalUniqueId TenantId, GlobalUniqueId ProjectId,
    GlobalUniqueId DiagramId, GlobalUniqueId UserId, UpdateDiagramDto Dto) : ICommand<DiagramDto>, IAuditableRequest<DiagramDto>
{
    public AuditLogRequest? AuditLog { get; set; }
    public AuditLogRequest? GetAuditLogRequest(DiagramDto response) => AuditLog;
}

public sealed record ListDiagramsQuery(GlobalUniqueId TenantId, GlobalUniqueId ProjectId, int Page, int PageSize, string? Search = null)
    : IQuery<DiagramListDto>;
public sealed record GetDiagramQuery(GlobalUniqueId TenantId, GlobalUniqueId ProjectId, GlobalUniqueId DiagramId)
    : IQuery<DiagramDto>;
public sealed record ListDiagramVersionsQuery(GlobalUniqueId TenantId, GlobalUniqueId ProjectId, GlobalUniqueId DiagramId)
    : IQuery<IReadOnlyList<DiagramVersionListItemDto>>;
public sealed record GetDiagramVersionQuery(GlobalUniqueId TenantId, GlobalUniqueId ProjectId,
    GlobalUniqueId DiagramId, int Revision) : IQuery<DiagramVersionDto>;
public sealed record CompareDiagramVersionsQuery(GlobalUniqueId TenantId, GlobalUniqueId ProjectId,
    GlobalUniqueId DiagramId, int Left, int Right) : IQuery<DiagramComparisonDto>;
