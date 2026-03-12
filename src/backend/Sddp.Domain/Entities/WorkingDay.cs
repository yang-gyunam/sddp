using Sddp.Abstractions.Base;
using Sddp.Abstractions.ValueObjects;

namespace Sddp.Domain.Entities;

/// <summary>
/// project workday settings
/// </summary>
public class WorkingDay : EntityBase
{
    public GlobalUniqueId TenantId { get; private set; }

    public GlobalUniqueId ProjectId { get; private set; }

    /// <summary>
    ///
    /// </summary>
    public DateOnly WorkDate { get; private set; }

    /// <summary>
    /// workday type (workday, offday, holiday, exception)
    /// </summary>
    public string DayType { get; private set; } = "workday";

    /// <summary>
    /// (:)
    /// </summary>
    public string? Note { get; private set; }

    public GlobalUniqueId CreatedBy { get; private set; }
    public GlobalUniqueId UpdatedBy { get; private set; }

    // EF Core default create
    private WorkingDay() { }

    public WorkingDay(
        GlobalUniqueId tenantId,
        GlobalUniqueId projectId,
        DateOnly workDate,
        string dayType,
        string? note,
        GlobalUniqueId createdBy)
    {
        TenantId = tenantId;
        ProjectId = projectId;
        WorkDate = workDate;
        DayType = ValidateDayType(dayType);
        Note = note;
        CreatedBy = createdBy;
        UpdatedBy = createdBy;
    }

    public void Update(string dayType, string? note, GlobalUniqueId updatedBy)
    {
        DayType = ValidateDayType(dayType);
        Note = note;
        UpdatedBy = updatedBy;
        MarkAsModified();
    }

    private static string ValidateDayType(string dayType)
    {
        var validTypes = new[] { "workday", "offday", "holiday", "exception" };
        return validTypes.Contains(dayType.ToLower()) ? dayType.ToLower() : "workday";
    }

    public bool IsWorkingDay => DayType == "workday" || DayType == "exception";
}
