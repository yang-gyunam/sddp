using Sddp.Abstractions.DTOs;
using Sddp.Abstractions.ValueObjects;
using Sddp.Application.Requests;

namespace Sddp.Application.Features.Diagrams;

public sealed record CreateDiagramV2Command(GlobalUniqueId TenantId, GlobalUniqueId ProjectId,
    GlobalUniqueId UserId, CreateDiagramV2Dto Dto) : ICommand<DiagramV2Dto>, IAuditableRequest<DiagramV2Dto>
{
    public AuditLogRequest? AuditLog { get; set; }
    public AuditLogRequest? GetAuditLogRequest(DiagramV2Dto response) => AuditLog;
}

public sealed record UpdateDiagramV2Command(GlobalUniqueId TenantId, GlobalUniqueId ProjectId,
    GlobalUniqueId DiagramId, GlobalUniqueId UserId, UpdateDiagramV2Dto Dto) : ICommand<DiagramV2Dto>, IAuditableRequest<DiagramV2Dto>
{
    public AuditLogRequest? AuditLog { get; set; }
    public AuditLogRequest? GetAuditLogRequest(DiagramV2Dto response) => AuditLog;
}

public sealed record ListDiagramsV2Query(GlobalUniqueId TenantId, GlobalUniqueId ProjectId, int Page, int PageSize, string? Search = null)
    : IQuery<DiagramV2ListDto>;
public sealed record GetDiagramV2Query(GlobalUniqueId TenantId, GlobalUniqueId ProjectId, GlobalUniqueId DiagramId)
    : IQuery<DiagramV2Dto>;
public sealed record ListDiagramVersionsV2Query(GlobalUniqueId TenantId, GlobalUniqueId ProjectId, GlobalUniqueId DiagramId)
    : IQuery<IReadOnlyList<DiagramVersionListItemDto>>;
public sealed record GetDiagramVersionV2Query(GlobalUniqueId TenantId, GlobalUniqueId ProjectId,
    GlobalUniqueId DiagramId, int Revision) : IQuery<DiagramV2VersionDto>;
public sealed record CompareDiagramVersionsV2Query(GlobalUniqueId TenantId, GlobalUniqueId ProjectId,
    GlobalUniqueId DiagramId, int Left, int Right) : IQuery<DiagramV2ComparisonDto>;
