using Sddp.Abstractions.Base;
using Sddp.Abstractions.ValueObjects;

namespace Sddp.Domain.Entities;

/// <summary>
/// User-defined task category.
/// Used for personal task classification without versioning or approval workflow.
/// </summary>
public class TaskCategory : EntityBase
{
    /// <summary>
    /// Tenant ID.
    /// </summary>
    public GlobalUniqueId TenantId { get; private set; }

    /// <summary>
    /// Owner ID.
    /// </summary>
    public GlobalUniqueId UserId { get; private set; }

    /// <summary>
    /// Category name.
    /// </summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>
    /// Icon name (lucide icon name).
    /// </summary>
    public string? Icon { get; private set; }

    /// <summary>
    /// Sort order.
    /// </summary>
    public int SortOrder { get; private set; }

    // Parameterless constructor for EF Core.
    private TaskCategory() { }

    public TaskCategory(
        GlobalUniqueId tenantId,
        GlobalUniqueId userId,
        string name,
        string? icon = null,
        int sortOrder = 0)
    {
        TenantId = tenantId;
        UserId = userId;
        Name = name.Trim();
        Icon = icon;
        SortOrder = sortOrder;
    }

    /// <summary>
    /// Updates category information.
    /// </summary>
    public void Update(string? name = null, string? icon = null, int? sortOrder = null)
    {
        if (name is not null) Name = name.Trim();
        if (icon is not null) Icon = icon;
        if (sortOrder.HasValue) SortOrder = sortOrder.Value;
        MarkAsModified();
    }
}
