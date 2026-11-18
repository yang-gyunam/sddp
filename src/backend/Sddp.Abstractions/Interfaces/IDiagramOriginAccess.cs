using Sddp.Abstractions.Models.Diagrams;

namespace Sddp.Abstractions.Interfaces;

/// <summary>Current scope, permission and private-membership checks for projected snapshots.</summary>
public interface IDiagramOriginAccess
{
    Task<HashSet<DiagramOrigin>> ReadableOrigins(IEnumerable<DiagramOrigin> origins, CancellationToken ct);
    Task<string> ReadLabel(DiagramOrigin origin, CancellationToken ct);
    Task RequireOrigins(DiagramOrigin[] origins, CancellationToken ct);
    Task RequireModel(DiagramRevision model, string? kind, CancellationToken ct);
    Task<bool> CanReadModel(DiagramRevision model, string kind, CancellationToken ct);
}
