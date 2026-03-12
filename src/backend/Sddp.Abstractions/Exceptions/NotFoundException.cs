namespace Sddp.Abstractions.Exceptions;

/// <summary>
/// Exception for missing entities that map to HTTP 404.
/// </summary>
public class NotFoundException : SddpException
{
    /// <summary>
    /// Type of the missing entity.
    /// </summary>
    public string EntityType { get; }

    /// <summary>
    /// Identifier of the missing entity.
    /// </summary>
    public string EntityId { get; }

    public NotFoundException(string entityType, string entityId)
        : base("NOT_FOUND", $"{entityType} with ID '{entityId}' was not found.")
    {
        EntityType = entityType;
        EntityId = entityId;
    }

    public NotFoundException(string entityType, Guid entityId)
        : this(entityType, entityId.ToString())
    {
    }
}
