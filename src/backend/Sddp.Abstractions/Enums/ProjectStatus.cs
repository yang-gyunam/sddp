namespace Sddp.Abstractions.Enums;

/// <summary>
/// Project lifecycle status
/// State transitions: Planning -> Active -> Concluded -> Archived
///                                   Concluded -> Active (Reopen)
/// </summary>
public enum ProjectStatus
{
    /// <summary>Planning phase - assembling members, configuring settings</summary>
    Planning = 0,

    /// <summary>Active phase - Spec/Requirement/Conversation creation available</summary>
    Active = 1,

    /// <summary>Concluded - read-only, can be reopened</summary>
    Concluded = 2,

    /// <summary>Archived - permanently read-only (terminal)</summary>
    Archived = 3
}

/// <summary>
/// ProjectStatus extension methods
/// </summary>
public static class ProjectStatusExtensions
{
    /// <summary>Can initialize (Planning -> Active)</summary>
    public static bool CanInitialize(this ProjectStatus status)
        => status == ProjectStatus.Planning;

    /// <summary>Can conclude (Active -> Concluded)</summary>
    public static bool CanConclude(this ProjectStatus status)
        => status == ProjectStatus.Active;

    /// <summary>Can reopen (Concluded -> Active)</summary>
    public static bool CanReopen(this ProjectStatus status)
        => status == ProjectStatus.Concluded;

    /// <summary>Can archive (Concluded -> Archived)</summary>
    public static bool CanArchive(this ProjectStatus status)
        => status == ProjectStatus.Concluded;

    /// <summary>Status allowing data creation/modification</summary>
    public static bool IsEditable(this ProjectStatus status)
        => status is ProjectStatus.Planning or ProjectStatus.Active;

    /// <summary>Read-only status</summary>
    public static bool IsReadOnly(this ProjectStatus status)
        => status is ProjectStatus.Concluded or ProjectStatus.Archived;
}
