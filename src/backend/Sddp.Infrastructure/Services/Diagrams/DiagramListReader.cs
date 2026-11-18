using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Sddp.Abstractions.Interfaces;
using Sddp.Abstractions.ValueObjects;
using Sddp.Domain.Entities;
using Sddp.Infrastructure.Persistence.Contexts;

namespace Sddp.Infrastructure.Services.Diagrams;

public sealed class DiagramListReader(SddpDbContext db, DiagramListModelCache cache, IStoredDiagramReader models) : IDiagramListReader
{
    private sealed record Header(Guid Id, string Title, string Kind, int Revision, DateTimeOffset UpdatedAt, string Hash)
    {
        internal DiagramListModelCache.Metadata? Metadata { get; set; }
    }

    public async Task<IReadOnlyList<DiagramListEntry>> ReadCandidatesAsync(GlobalUniqueId tenant, GlobalUniqueId project,
        string? search, CancellationToken ct)
    {
        if (!db.Database.IsNpgsql()) return await ReadCurrentAsync(tenant, project, search, ct);

        // Both the row fields and the content fingerprint come from one statement snapshot.
        // An update or a project restore may reuse the same diagram ID and revision.
        var headers = await db.Database.SqlQuery<Header>($"""
            -- Diagram list headers
            SELECT id AS "Id", title AS "Title", kind AS "Kind", revision AS "Revision", updated_at AS "UpdatedAt",
                   encode(model_hash, 'hex') AS "Hash"
            FROM diagrams
            WHERE tenant_id={tenant.ToGuid()} AND project_id={project.ToGuid()} AND is_active
              AND ({search}::text IS NULL OR strpos(lower(title), {search}::text) > 0)
            ORDER BY updated_at DESC, id
            """).ToListAsync(ct);

        var missing = new Dictionary<GlobalUniqueId, Header>();
        foreach (var header in headers)
        {
            header.Metadata = cache.Get(header.Kind, header.Hash);
            if (header.Metadata is null) missing.Add(header.Id, header);
        }

        if (missing.Count > 0)
        {
            var ids = missing.Keys.ToArray();
            var bodies = db.Set<Diagram>().AsNoTracking().TagWith("Diagram list model bodies")
                .Where(d => d.IsActive && d.TenantId == tenant && d.ProjectId == project && ids.Contains(d.Id))
                .Select(d => new { d.Id, d.ModelJson });
            await foreach (var row in bodies.AsAsyncEnumerable().WithCancellation(ct))
            {
                var header = missing[row.Id];
                var hash = Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(row.ModelJson)));
                if (hash != header.Hash) continue;
                var metadata = DiagramListModelCache.From(models.Inspect(row.Id, row.ModelJson, header.Kind));
                header.Metadata = metadata;
                cache.Remember(header.Kind, hash, metadata);
            }
            // A cache miss was deleted or changed between statements. Read one coherent snapshot.
            if (missing.Values.Any(h => h.Metadata is null)) return await ReadCurrentAsync(tenant, project, search, ct);
        }

        var result = new List<DiagramListEntry>(headers.Count);
        foreach (var header in headers)
        {
            var metadata = header.Metadata!;
            if (!metadata.Origins.IsDefault)
                result.Add(new(new(header.Id.ToString(), header.Title, header.Kind, header.Revision,
                    header.UpdatedAt, metadata.Schema, metadata.DiagramType), metadata.Origins));
        }
        return result;
    }

    private async Task<IReadOnlyList<DiagramListEntry>> ReadCurrentAsync(GlobalUniqueId tenant, GlobalUniqueId project,
        string? search, CancellationToken ct)
    {
        var rows = db.Set<Diagram>().AsNoTracking()
            .Where(d => d.IsActive && d.TenantId == tenant && d.ProjectId == project &&
                (search == null || d.Title.ToLower().Contains(search)))
            .OrderByDescending(d => d.UpdatedAt).ThenBy(d => d.Id)
            .Select(d => new { d.Id, d.Title, d.Revision, d.Kind, d.UpdatedAt, d.ModelJson });
        var result = new List<DiagramListEntry>();
        await foreach (var row in rows.AsAsyncEnumerable().WithCancellation(ct))
        {
            var metadata = DiagramListModelCache.From(models.Inspect(row.Id, row.ModelJson, row.Kind));
            if (!metadata.Origins.IsDefault)
                result.Add(new(new(row.Id.ToString(), row.Title, row.Kind, row.Revision, row.UpdatedAt.ToDateTimeOffset(),
                    metadata.Schema, metadata.DiagramType), metadata.Origins));
        }
        return result;
    }
}
