using System.Collections.Immutable;
using Microsoft.Extensions.Caching.Memory;
using Sddp.Abstractions.Models.Diagrams;
using Sddp.Abstractions.Interfaces;

namespace Sddp.Infrastructure.Services.Diagrams;

/// <summary>Immutable model facts only. Membership and permissions are resolved on each request.</summary>
public sealed class DiagramListModelCache : IDisposable
{
    // A slot accounts for an entry or one referenced origin. Source text is never retained.
    private readonly MemoryCache _cache = new(new MemoryCacheOptions { SizeLimit = 32_768 });

    internal sealed record Metadata(int Schema, string DiagramType, ImmutableArray<DiagramOrigin> Origins);

    internal Metadata? Get(string kind, string hash) => _cache.Get<Metadata>((kind, hash));

    internal void Remember(string kind, string hash, Metadata metadata) =>
        _cache.Set((kind, hash), metadata, new MemoryCacheEntryOptions
        {
            Size = metadata.Origins.IsDefault ? 1 : 1 + metadata.Origins.Length,
            SlidingExpiration = TimeSpan.FromMinutes(10)
        });

    internal static Metadata From(InspectedDiagram? inspected) => inspected is null
        ? new(0, string.Empty, default)
        : new(inspected.Document.StoredSchemaVersion, inspected.Document.Model.DiagramType, [.. inspected.Origins]);

    public void Dispose() => _cache.Dispose();
}
