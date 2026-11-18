using System.Collections.Immutable;
using Sddp.Abstractions.DTOs;
using Sddp.Abstractions.Enums;
using Sddp.Abstractions.Models.Diagrams;
using Sddp.Abstractions.ValueObjects;

namespace Sddp.Abstractions.Interfaces;

public sealed record DiagramListEntry(DiagramV2ListItemDto Item, ImmutableArray<DiagramOrigin> Origins);

public interface IDiagramListReader
{
    Task<IReadOnlyList<DiagramListEntry>> ReadCandidatesAsync(GlobalUniqueId tenant, GlobalUniqueId project,
        string? search, CancellationToken ct);
}

public interface IDiagramOriginScopeReader
{
    Task<HashSet<GlobalUniqueId>> ReadScopedOriginIdsAsync(EntityType type, GlobalUniqueId[] ids,
        GlobalUniqueId tenant, GlobalUniqueId project, GlobalUniqueId? user, CancellationToken ct);
}
