namespace Sddp.Abstractions.Interfaces.Snapshots;

/// <summary>
/// Wrapper for the full snapshot JSONB payload.
/// Each key is a table name, value is a list of row dictionaries.
/// </summary>
public sealed class ProjectSnapshotData
{
    public int FormatVersion { get; set; } = 1;
    public Dictionary<string, List<Dictionary<string, object?>>> Tables { get; set; } = new();
    public Dictionary<string, ProjectSnapshotExtensionData> Extensions { get; set; } = new();
}
