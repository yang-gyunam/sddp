using System.Text.Json;
using Microsoft.Extensions.Logging;
using Sddp.Abstractions.Interfaces;
using Sddp.Abstractions.Models.Diagrams;
using Sddp.Abstractions.Models.Diagrams.V2;
using Sddp.Abstractions.ValueObjects;

namespace Sddp.Infrastructure.Services.Diagrams;

public sealed class StoredDiagramReader(ILogger<StoredDiagramReader> logger) : IStoredDiagramReader
{
    public InspectedDiagram? Inspect(GlobalUniqueId diagramId, string json, string kind)
    {
        StoredDiagramModel2 stored;
        try
        {
            stored = DiagramStorage2.Read(json);
        }
        catch (Exception ex) when (ex is JsonException or NotSupportedException or InvalidDataException)
        {
            // Converter and validation messages may contain source data. Log only the category.
            logger.LogWarning("Unreadable stored diagram {DiagramId}: model decoding/validation ({FailureType})", diagramId, ex.GetType().Name);
            return null;
        }

        try
        {
            var origins = DiagramProjectionSource.ReadForKind(DiagramStorage2.AccessView(stored.Model), kind);
            return new(stored, origins ?? []);
        }
        catch (InvalidDataException)
        {
            logger.LogWarning("Unreadable stored diagram {DiagramId}: invalid projection provenance", diagramId);
            return null;
        }
    }
}
