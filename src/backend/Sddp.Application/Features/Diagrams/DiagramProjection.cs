using Sddp.Abstractions.DTOs;
using Sddp.Abstractions.Exceptions;
using Sddp.Abstractions.Interfaces;
using Sddp.Abstractions.Models.Diagrams;
using Sddp.Abstractions.ValueObjects;
using Sddp.Domain.Entities;

namespace Sddp.Application.Features.Diagrams;

/// <summary>Read-only projection of selected entities and their current relationships.</summary>
public sealed class DiagramProjection(IUnitOfWork unitOfWork, IDiagramOriginAccess access)
{
    public async Task<DiagramRevision> Build(GlobalUniqueId tenant, GlobalUniqueId project,
        DiagramProjectionDto dto, CancellationToken ct)
    {
        try { DiagramProjectionSource.ValidateOrigins(dto.Origins); }
        catch (InvalidDataException ex) { throw new ValidationException("origins", ex.Message); }
        var origins = dto.Origins.OrderBy(o => o.Type).ThenBy(o => o.Id, StringComparer.Ordinal).ToArray();
        var nodes = new Dictionary<string, DiagramNode>(StringComparer.Ordinal);
        foreach (var origin in origins)
            nodes.Add(origin.Key, new DiagramNode { Label = await access.ReadLabel(origin, ct), Presentation = new() });
        var relationships = await unitOfWork.Repository<Relationship>().FindAsync(r =>
            r.TenantId == tenant && r.ProjectId == project && r.IsActive && r.ValidTo == null, ct);
        var edges = new Dictionary<string, DiagramEdge>(StringComparer.Ordinal);
        foreach (var r in relationships.OrderBy(r => r.Id))
        {
            var from = $"{(int)r.FromEntityType}:{r.FromEntityId}";
            var to = $"{(int)r.ToEntityType}:{r.ToEntityId}";
            if (nodes.ContainsKey(from) && nodes.ContainsKey(to))
                edges.Add(r.Id.ToString(), new DiagramEdge { From = from, To = to, Label = r.Type.ToString(), Presentation = new() });
        }
        return new DiagramRevision { SchemaVersion = 1, DiagramId = "new", Revision = 1,
            Source = DiagramProjectionSource.Create(origins), Presentation = new(), Nodes = nodes, Edges = edges };
    }
}
