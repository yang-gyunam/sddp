using Sddp.Abstractions.Models.Diagrams;
using Sddp.Abstractions.Models.Diagrams.V2;
using Sddp.Abstractions.ValueObjects;

namespace Sddp.Abstractions.Interfaces;

public sealed record InspectedDiagram(StoredDiagramModel2 Document, DiagramOrigin[] Origins);

public interface IStoredDiagramReader
{
    // Null means the stored document cannot supply trustworthy model or origin information.
    InspectedDiagram? Inspect(GlobalUniqueId diagramId, string json, string kind);
}
