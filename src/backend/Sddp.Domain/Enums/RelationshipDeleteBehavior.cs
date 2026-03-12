namespace Sddp.Domain.Enums;

/// <summary>
/// relationship delete
/// </summary>
public enum RelationshipDeleteBehavior
{
    Cascade = 0,
    Restrict = 1,
    SetNull = 2,
    NoAction = 3
}
