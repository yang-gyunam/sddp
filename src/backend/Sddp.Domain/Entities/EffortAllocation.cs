using Sddp.Abstractions.Base;
using Sddp.Abstractions.ValueObjects;

namespace Sddp.Domain.Entities;

/// <summary>
/// project user
/// </summary>
public class EffortAllocation : EntityBase
{
    public GlobalUniqueId TenantId { get; private set; }

    public GlobalUniqueId ProjectId { get; private set; }

    public GlobalUniqueId UserId { get; private set; }

    /// <summary>
    ///
    /// </summary>
    public DateOnly AllocationDate { get; private set; }

    /// <summary>
    /// (0-24)
    /// </summary>
    public decimal AllocatedHours { get; private set; }

    public GlobalUniqueId CreatedBy { get; private set; }
    public GlobalUniqueId UpdatedBy { get; private set; }

    // Navigation properties
    public User? User { get; private set; }

    // EF Core default create
    private EffortAllocation() { }

    public EffortAllocation(
        GlobalUniqueId tenantId,
        GlobalUniqueId projectId,
        GlobalUniqueId userId,
        DateOnly allocationDate,
        decimal allocatedHours,
        GlobalUniqueId createdBy)
    {
        TenantId = tenantId;
        ProjectId = projectId;
        UserId = userId;
        AllocationDate = allocationDate;
        AllocatedHours = Math.Clamp(allocatedHours, 0, 24);
        CreatedBy = createdBy;
        UpdatedBy = createdBy;
    }

    public void UpdateAllocatedHours(decimal hours, GlobalUniqueId updatedBy)
    {
        AllocatedHours = Math.Clamp(hours, 0, 24);
        UpdatedBy = updatedBy;
        MarkAsModified();
    }
}
