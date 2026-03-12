namespace Sddp.Abstractions.Enums;

/// <summary>
/// Relationship types between Specs (8 types)
/// </summary>
public enum RelationType
{
    /// <summary>Supersedes - completely replaces a previous version</summary>
    Supersedes = 0,

    /// <summary>Evolves From - evolved from a previous version</summary>
    EvolvesFrom = 1,

    /// <summary>Extends - extends an existing spec</summary>
    Extends = 2,

    /// <summary>Conflicts With - conflicts with another spec</summary>
    ConflictsWith = 3,

    /// <summary>Depends On - depends on another spec</summary>
    DependsOn = 4,

    /// <summary>Implements - implements a requirement</summary>
    Implements = 5,

    /// <summary>Replaces - functionally replaces</summary>
    Replaces = 6,

    /// <summary>Affects - affects another spec</summary>
    Affects = 7
}
