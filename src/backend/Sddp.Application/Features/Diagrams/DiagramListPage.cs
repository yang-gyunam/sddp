using Sddp.Abstractions.DTOs;
using Sddp.Abstractions.Exceptions;
using Sddp.Abstractions.Interfaces;
using Sddp.Abstractions.ValueObjects;

namespace Sddp.Application.Features.Diagrams;

internal static class DiagramListPage
{
    public static async Task<DiagramV2ListDto> Read(IDiagramListReader lists, IDiagramOriginAccess origins,
        GlobalUniqueId tenant, GlobalUniqueId project, string? searchText, int pageNumber, int pageSize,
        bool legacyOnly, CancellationToken ct)
    {
        if (pageNumber < 1 || pageSize is < 1 or > 100)
            throw new ValidationException("page", "Page must be positive and pageSize must be between 1 and 100.");
        if (searchText?.Length > 200) throw new ValidationException("search", "Maximum length is 200.");
        var search = string.IsNullOrWhiteSpace(searchText) ? null : searchText.Trim().ToLowerInvariant();
        var all = await lists.ReadCandidatesAsync(tenant, project, search, ct);
        var candidates = legacyOnly ? all.Where(entry => entry.Item.StoredSchemaVersion == 1) : all;
        var readableOrigins = await origins.ReadableOrigins(candidates.SelectMany(entry => entry.Origins), ct);
        var offset = (long)(pageNumber - 1) * pageSize;
        var page = new List<DiagramV2ListItemDto>(pageSize);
        var readableCount = 0;
        foreach (var entry in candidates)
        {
            ct.ThrowIfCancellationRequested();
            if (!entry.Origins.All(readableOrigins.Contains)) continue;
            if (readableCount >= offset && page.Count < pageSize) page.Add(entry.Item);
            readableCount++;
        }
        return new DiagramV2ListDto(page, readableCount, pageNumber, pageSize);
    }
}
