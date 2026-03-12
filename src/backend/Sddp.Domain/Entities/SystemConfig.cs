using Sddp.Abstractions.Base;
using Sddp.Abstractions.ValueObjects;

namespace Sddp.Domain.Entities;

/// <summary>
/// System configuration entity
/// tenant_id NULL = global system config
/// tenant_id NOT NULL, project_id NULL = tenant-level config
/// tenant_id NOT NULL, project_id NOT NULL = project-level config
/// </summary>
public class SystemConfig : EntityBase
{
    /// <summary>
    /// Tenant ID (NULL = global config)
    /// </summary>
    public GlobalUniqueId? TenantId { get; set; }

    /// <summary>
    /// Project ID (NULL = tenant-level config)
    /// </summary>
    public GlobalUniqueId? ProjectId { get; set; }

    /// <summary>
    /// Configuration group (general, auth, storage, performance, aiAgent)
    /// </summary>
    public string ConfigGroup { get; set; } = string.Empty;

    /// <summary>
    /// Configuration key
    /// </summary>
    public string ConfigKey { get; set; } = string.Empty;

    /// <summary>
    /// Configuration value (stored as a string)
    /// </summary>
    public string? ConfigValue { get; set; }

    /// <summary>
    /// Value type (string, number, boolean, json)
    /// </summary>
    public string ValueType { get; set; } = "string";

    /// <summary>
    /// Display name
    /// </summary>
    public string? DisplayName { get; set; }

    /// <summary>
    /// Description
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Whether this value is sensitive (password, etc.)
    /// </summary>
    public bool IsSensitive { get; set; }

    /// <summary>
    /// Whether this value is read-only (system-managed)
    /// </summary>
    public bool IsReadonly { get; set; }

    /// <summary>
    /// Whether this is a system config (cannot be deleted)
    /// </summary>
    public bool IsSystem { get; set; }

    /// <summary>
    /// Sort order
    /// </summary>
    public int SortOrder { get; set; }

    /// <summary>
    /// Creator ID
    /// </summary>
    public GlobalUniqueId? CreatedBy { get; set; }

    /// <summary>
    /// Updater ID
    /// </summary>
    public GlobalUniqueId? UpdatedBy { get; set; }

    /// <summary>
    /// Updates the configuration value
    /// </summary>
    public void UpdateValue(string? value, GlobalUniqueId? updatedBy = null)
    {
        ConfigValue = value;
        UpdatedBy = updatedBy;
        MarkAsModified();
    }
}
