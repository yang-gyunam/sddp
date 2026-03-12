using Sddp.Abstractions.DTOs;
using Sddp.Domain.Entities;

namespace Sddp.Application.Features.Effort;

internal static class EffortMapping
{
    internal static EffortAllocationDto MapToAllocationDto(EffortAllocation allocation) => new()
    {
        Id = allocation.Id.ToGuid(),
        UserId = allocation.UserId.ToGuid(),
        ProjectId = allocation.ProjectId.ToGuid(),
        Date = allocation.AllocationDate.ToString("yyyy-MM-dd"),
        AllocatedHours = allocation.AllocatedHours,
        CreatedAt = allocation.CreatedAt,
        UpdatedAt = allocation.UpdatedAt,
        CreatedBy = allocation.CreatedBy.ToGuid(),
        UpdatedBy = allocation.UpdatedBy.ToGuid()
    };

    internal static WorklogDto MapToWorklogDto(Worklog worklog) => new()
    {
        Id = worklog.Id.ToGuid(),
        UserId = worklog.UserId.ToGuid(),
        ProjectId = worklog.ProjectId.ToGuid(),
        Date = worklog.WorkDate.ToString("yyyy-MM-dd"),
        SpentHours = worklog.SpentHours,
        Note = worklog.Note,
        TaskId = worklog.TaskId?.ToGuid(),
        CreatedAt = worklog.CreatedAt,
        UpdatedAt = worklog.UpdatedAt
    };

    internal static WorkingDayDto MapToWorkingDayDto(WorkingDay workingDay) => new()
    {
        Id = workingDay.Id.ToGuid(),
        ProjectId = workingDay.ProjectId.ToGuid(),
        Date = workingDay.WorkDate.ToString("yyyy-MM-dd"),
        Type = workingDay.DayType,
        Note = workingDay.Note,
        CreatedAt = workingDay.CreatedAt,
        UpdatedAt = workingDay.UpdatedAt
    };
}
