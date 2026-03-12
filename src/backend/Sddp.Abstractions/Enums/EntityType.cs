namespace Sddp.Abstractions.Enums;

/// <summary>
/// relationship entity type
/// REQ-10.3: relationship independent entity
/// </summary>
public enum EntityType
{
    /// <summary>spec</summary>
    Spec = 0,

    /// <summary>requirement</summary>
    Requirement = 1,

    /// <summary>Forum Topic</summary>
    Topic = 2,

    /// <summary>glossary (Glossary Term)</summary>
    GlossaryTerm = 3,

    /// <summary>Conversation (Channel, Forum, DirectMessage)</summary>
    Conversation = 4,

    /// <summary>Artifact</summary>
    Artifact = 5,

    /// <summary>Task</summary>
    Task = 6
}
