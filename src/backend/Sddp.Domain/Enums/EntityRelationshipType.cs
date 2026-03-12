namespace Sddp.Domain.Enums;

/// <summary>
/// Entity metadata relationship type
/// </summary>
public enum EntityRelationshipType
{
    /// <summary>N:1 relationship (FK exists on this entity)</summary>
    ManyToOne = 0,

    /// <summary>1:N relationship (only collection navigation is generated)</summary>
    OneToMany = 1
}
