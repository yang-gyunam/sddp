using Sddp.Abstractions.ValueObjects;

namespace Sddp.Abstractions.Interfaces;

public interface IProjectSearchFileIndex
{
    /// <summary>Validates repository access and atomically indexes the current committed tree.</summary>
    Task<string> PrepareAsync(GlobalUniqueId tenant, GlobalUniqueId project, CancellationToken ct);
}
