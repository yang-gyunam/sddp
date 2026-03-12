using Sddp.Abstractions.Base;
using Sddp.Abstractions.ValueObjects;

namespace Sddp.Domain.Entities;

/// <summary>
/// Entity metadata describing entities to generate automatically
/// Input source for the Roslyn generator
/// </summary>
public class EntityMetadata : AuditableEntityBase
{
    /// <summary>
    /// Tenant ID (for multi-tenancy)
    /// </summary>
    public GlobalUniqueId TenantId { get; private set; }

    /// <summary>
    /// Project ID
    /// </summary>
    public GlobalUniqueId ProjectId { get; private set; }

    /// <summary>
    /// Linked spec ID
    /// </summary>
    public GlobalUniqueId SpecId { get; private set; }

    /// <summary>
    /// Entity name (PascalCase, for example: Employee)
    /// </summary>
    public string EntityName { get; private set; } = string.Empty;

    /// <summary>
    /// Table name (snake_case, for example: employees)
    /// </summary>
    public string TableName { get; private set; } = string.Empty;

    /// <summary>
    /// Namespace (for example: Sddp.Domain.Entities)
    /// </summary>
    public string Namespace { get; private set; } = "Sddp.Domain.Entities";

    /// <summary>
    /// Entity description
    /// </summary>
    public string Description { get; private set; } = string.Empty;

    /// <summary>
    /// Base class (AuditableEntityBase, EntityBase, VersionedEntityBase)
    /// </summary>
    public string BaseClass { get; private set; } = "AuditableEntityBase";

    /// <summary>
    /// Whether generation is enabled (false means disabled)
    /// </summary>
    public bool IsGenerated { get; private set; } = true;

    /// <summary>
    /// Linked spec
    /// </summary>
    public Spec Spec { get; private set; } = null!;

    /// <summary>
    /// Field metadata list
    /// </summary>
    public ICollection<FieldMetadata> Fields { get; private set; } = new List<FieldMetadata>();

    /// <summary>
    /// Relationship metadata list
    /// </summary>
    public ICollection<EntityRelationshipMetadata> Relationships { get; private set; } = new List<EntityRelationshipMetadata>();

    // Default constructor for EF Core
    private EntityMetadata() { }

    /// <summary>
    /// Creates EntityMetadata
    /// </summary>
    public EntityMetadata(
        GlobalUniqueId tenantId,
        GlobalUniqueId projectId,
        GlobalUniqueId specId,
        string entityName,
        string tableName,
        string description = "",
        string @namespace = "Sddp.Domain.Entities",
        string baseClass = "AuditableEntityBase")
    {
        TenantId = tenantId;
        ProjectId = projectId;
        SpecId = specId;
        EntityName = entityName;
        TableName = tableName;
        Description = description;
        Namespace = @namespace;
        BaseClass = baseClass;
        IsGenerated = true;
    }

    /// <summary>
    /// Updates entity metadata
    /// </summary>
    public void Update(
        string entityName,
        string tableName,
        string description,
        string @namespace,
        string baseClass)
    {
        EntityName = entityName;
        TableName = tableName;
        Description = description;
        Namespace = @namespace;
        BaseClass = baseClass;
        MarkAsModified();
    }

    /// <summary>
    /// Adds a field
    /// </summary>
    public void AddField(FieldMetadata field)
    {
        Fields.Add(field);
        MarkAsModified();
    }

    /// <summary>
    /// Removes a field
    /// </summary>
    public void RemoveField(FieldMetadata field)
    {
        Fields.Remove(field);
        MarkAsModified();
    }

    /// <summary>
    /// Adds a relationship
    /// </summary>
    public void AddRelationship(EntityRelationshipMetadata relationship)
    {
        Relationships.Add(relationship);
        MarkAsModified();
    }

    /// <summary>
    /// Removes a relationship
    /// </summary>
    public void RemoveRelationship(EntityRelationshipMetadata relationship)
    {
        Relationships.Remove(relationship);
        MarkAsModified();
    }

    /// <summary>
    /// Enables or disables generation
    /// </summary>
    public void SetGenerationEnabled(bool enabled)
    {
        IsGenerated = enabled;
        MarkAsModified();
    }
}
