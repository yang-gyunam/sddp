namespace Sddp.Domain.Enums;

/// <summary>
/// Data type of a metadata field
/// </summary>
public enum FieldType
{
    /// <summary>
    /// String (C#: string, PostgreSQL: text)
    /// </summary>
    String = 0,

    /// <summary>
    /// Integer (C#: int, PostgreSQL: integer)
    /// </summary>
    Int = 1,

    /// <summary>
    /// Long integer (C#: long, PostgreSQL: bigint)
    /// </summary>
    Long = 2,

    /// <summary>
    /// Decimal (C#: decimal, PostgreSQL: numeric)
    /// </summary>
    Decimal = 3,

    /// <summary>
    /// Boolean (C#: bool, PostgreSQL: boolean)
    /// </summary>
    Boolean = 4,

    /// <summary>
    /// Date/Time (C#: Timestamp, PostgreSQL: timestamptz)
    /// </summary>
    DateTime = 5,

    /// <summary>
    /// GUID (C#: GlobalUniqueId, PostgreSQL: uuid)
    /// </summary>
    Guid = 6,

    /// <summary>
    /// JSON (C#: string, PostgreSQL: jsonb)
    /// </summary>
    Json = 7
}
