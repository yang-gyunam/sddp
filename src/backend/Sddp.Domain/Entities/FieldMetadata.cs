using Sddp.Abstractions.Base;
using Sddp.Abstractions.ValueObjects;
using Sddp.Domain.Enums;

namespace Sddp.Domain.Entities;

/// <summary>
/// Field metadata describing an entity field
/// </summary>
public class FieldMetadata : EntityBase
{
    /// <summary>
    /// Linked EntityMetadata ID
    /// </summary>
    public GlobalUniqueId EntityMetadataId { get; private set; }

    /// <summary>
    /// Field name (PascalCase, for example: EmployeeName)
    /// </summary>
    public string FieldName { get; private set; } = string.Empty;

    /// <summary>
    /// Column name (snake_case, for example: employee_name)
    /// </summary>
    public string ColumnName { get; private set; } = string.Empty;

    /// <summary>
    /// Field type (String, Int, DateTime, etc.)
    /// </summary>
    public FieldType FieldType { get; private set; }

    /// <summary>
    /// Whether the field is required
    /// </summary>
    public bool IsRequired { get; private set; }

    /// <summary>
    /// Whether the field has a unique constraint
    /// </summary>
    public bool IsUnique { get; private set; }

    /// <summary>
    /// Maximum length (applies only to string types)
    /// </summary>
    public int? MaxLength { get; private set; }

    /// <summary>
    /// Minimum length (applies only to string types)
    /// </summary>
    public int? MinLength { get; private set; }

    /// <summary>
    /// Validation type (Email, Phone, Url, Pattern, etc.)
    /// </summary>
    public string? ValidationType { get; private set; }

    /// <summary>
    /// Custom regex pattern (ValidationType=Pattern)
    /// </summary>
    public string? Pattern { get; private set; }

    /// <summary>
    /// Default value
    /// </summary>
    public string? DefaultValue { get; private set; }

    /// <summary>
    /// Field description
    /// </summary>
    public string Description { get; private set; } = string.Empty;

    /// <summary>
    /// Display order (UI ordering)
    /// </summary>
    public int DisplayOrder { get; private set; }

    /// <summary>
    /// Linked EntityMetadata
    /// </summary>
    public EntityMetadata EntityMetadata { get; private set; } = null!;

    // Default constructor for EF Core
    private FieldMetadata() { }

    /// <summary>
    /// Creates FieldMetadata
    /// </summary>
    public FieldMetadata(
        GlobalUniqueId entityMetadataId,
        string fieldName,
        string columnName,
        FieldType fieldType,
        bool isRequired = false,
        bool isUnique = false,
        int? maxLength = null,
        int? minLength = null,
        string? validationType = null,
        string? pattern = null,
        string? defaultValue = null,
        string description = "",
        int displayOrder = 0)
    {
        EntityMetadataId = entityMetadataId;
        FieldName = fieldName;
        ColumnName = columnName;
        FieldType = fieldType;
        IsRequired = isRequired;
        IsUnique = isUnique;
        MaxLength = maxLength;
        MinLength = minLength;
        ValidationType = validationType;
        Pattern = pattern;
        DefaultValue = defaultValue;
        Description = description;
        DisplayOrder = displayOrder;
    }

    /// <summary>
    /// Updates field metadata
    /// </summary>
    public void Update(
        string fieldName,
        string columnName,
        FieldType fieldType,
        bool isRequired,
        bool isUnique,
        int? maxLength,
        int? minLength,
        string? validationType,
        string? pattern,
        string? defaultValue,
        string description,
        int displayOrder)
    {
        FieldName = fieldName;
        ColumnName = columnName;
        FieldType = fieldType;
        IsRequired = isRequired;
        IsUnique = isUnique;
        MaxLength = maxLength;
        MinLength = minLength;
        ValidationType = validationType;
        Pattern = pattern;
        DefaultValue = defaultValue;
        Description = description;
        DisplayOrder = displayOrder;
        MarkAsModified();
    }
}
