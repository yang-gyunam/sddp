using Sddp.Abstractions.Base;
using Sddp.Abstractions.ValueObjects;
using Sddp.Domain.Enums;

namespace Sddp.Domain.Entities;

/// <summary>
/// Relationship metadata between entities
/// </summary>
public class EntityRelationshipMetadata : EntityBase
{
    /// <summary>
    /// Linked EntityMetadata ID
    /// </summary>
    public GlobalUniqueId EntityMetadataId { get; private set; }

    /// <summary>
    /// Relationship type (ManyToOne, OneToMany)
    /// </summary>
    public EntityRelationshipType RelationshipType { get; private set; }

    /// <summary>
    /// Target entity name (PascalCase)
    /// </summary>
    public string TargetEntity { get; private set; } = string.Empty;

    /// <summary>
    /// FK property name (ManyToOne only)
    /// </summary>
    public string? ForeignKeyName { get; private set; }

    /// <summary>
    /// FK column name (ManyToOne only)
    /// </summary>
    public string? ForeignKeyColumn { get; private set; }

    /// <summary>
    /// Navigation property name
    /// </summary>
    public string NavigationName { get; private set; } = string.Empty;

    /// <summary>
    /// Inverse navigation property name (optional)
    /// </summary>
    public string? InverseNavigationName { get; private set; }

    /// <summary>
    /// Whether the relationship is required (ManyToOne only)
    /// </summary>
    public bool IsRequired { get; private set; }

    /// <summary>
    /// Delete behavior
    /// </summary>
    public RelationshipDeleteBehavior OnDelete { get; private set; } = RelationshipDeleteBehavior.Restrict;

    /// <summary>
    /// Description
    /// </summary>
    public string Description { get; private set; } = string.Empty;

    /// <summary>
    /// Display order
    /// </summary>
    public int DisplayOrder { get; private set; }

    /// <summary>
    /// Linked EntityMetadata
    /// </summary>
    public EntityMetadata EntityMetadata { get; private set; } = null!;

    // Default constructor for EF Core
    private EntityRelationshipMetadata() { }

    /// <summary>
    /// Creates EntityRelationshipMetadata
    /// </summary>
    public EntityRelationshipMetadata(
        GlobalUniqueId entityMetadataId,
        EntityRelationshipType relationshipType,
        string targetEntity,
        string navigationName,
        string? foreignKeyName = null,
        string? foreignKeyColumn = null,
        string? inverseNavigationName = null,
        bool isRequired = false,
        RelationshipDeleteBehavior onDelete = RelationshipDeleteBehavior.Restrict,
        string description = "",
        int displayOrder = 0)
    {
        EntityMetadataId = entityMetadataId;
        RelationshipType = relationshipType;
        TargetEntity = targetEntity;
        NavigationName = navigationName;
        ForeignKeyName = foreignKeyName;
        ForeignKeyColumn = foreignKeyColumn;
        InverseNavigationName = inverseNavigationName;
        IsRequired = isRequired;
        OnDelete = onDelete;
        Description = description;
        DisplayOrder = displayOrder;
    }

    /// <summary>
    /// Updates relationship metadata
    /// </summary>
    public void Update(
        EntityRelationshipType relationshipType,
        string targetEntity,
        string navigationName,
        string? foreignKeyName,
        string? foreignKeyColumn,
        string? inverseNavigationName,
        bool isRequired,
        RelationshipDeleteBehavior onDelete,
        string description,
        int displayOrder)
    {
        RelationshipType = relationshipType;
        TargetEntity = targetEntity;
        NavigationName = navigationName;
        ForeignKeyName = foreignKeyName;
        ForeignKeyColumn = foreignKeyColumn;
        InverseNavigationName = inverseNavigationName;
        IsRequired = isRequired;
        OnDelete = onDelete;
        Description = description;
        DisplayOrder = displayOrder;
        MarkAsModified();
    }
}
