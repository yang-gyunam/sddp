using Sddp.Abstractions.Enums;
using Sddp.Abstractions.ValueObjects;

namespace Sddp.Abstractions.Interfaces;

public readonly record struct RelationshipEndpoint(EntityType Type, GlobalUniqueId Id);
public sealed record EndpointDisplay(string Label, string? Code, string? Status);

public interface IRelationshipEndpointAccess
{
    Task<EndpointDisplay> RequireRootAsync(GlobalUniqueId tenant, GlobalUniqueId project, RelationshipEndpoint root, CancellationToken ct);
    Task<IReadOnlyDictionary<RelationshipEndpoint, EndpointDisplay>> ReadAsync(GlobalUniqueId tenant, GlobalUniqueId project,
        IEnumerable<RelationshipEndpoint> candidates, CancellationToken ct);
}

// This projection stays inside the authorization boundary. Diagram models carry their origin restrictions.
public sealed class ScopedRelationshipEndpoint
{
    public GlobalUniqueId Id { get; init; }
    public required string Label { get; init; }
    public string? Code { get; init; }
    public string? Status { get; init; }
    public string? DiagramKind { get; init; }
    public string? ModelJson { get; init; }
}

public interface IRelationshipEndpointReader
{
    Task<List<ScopedRelationshipEndpoint>> ReadScopedAsync(EntityType type, GlobalUniqueId[] ids, GlobalUniqueId tenant,
        GlobalUniqueId project, GlobalUniqueId? user, CancellationToken ct);
}
