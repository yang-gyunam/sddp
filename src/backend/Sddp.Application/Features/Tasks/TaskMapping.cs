using Sddp.Abstractions.DTOs;
using Sddp.Abstractions.Interfaces;
using Sddp.Abstractions.ValueObjects;
using Sddp.Application.Utilities;
using Sddp.Domain.Entities;

namespace Sddp.Application.Features.Tasks;

internal static class TaskMapping
{
    internal static TaskItemDto MapToDto(TaskItem task, UserRefDto? assignee, int linkedItemCount)
    {
        return new TaskItemDto(
            Id: task.Id.ToString(),
            TenantId: task.TenantId.ToString(),
            ProjectId: task.ProjectId?.ToString(),
            Title: task.Title,
            Description: task.Description,
            Status: task.Status,
            Priority: task.Priority,
            Assignee: assignee,
            Creator: UserRefHelper.ToUserRef(task.CreatorId.ToString(), null, null),
            EstimatedHours: task.EstimatedHours,
            ActualHours: task.ActualHours,
            LinkedItemCount: linkedItemCount,
            SortOrder: task.SortOrder,
            DueDate: task.DueDate,
            CategoryId: task.CategoryId?.ToString(),
            CreatedAt: task.CreatedAt.ToDateTimeOffset(),
            UpdatedAt: task.UpdatedAt.ToDateTimeOffset(),
            CompletedAt: task.CompletedAt?.ToDateTimeOffset());
    }

    internal static async Task<TaskItemDetailDto> MapToDetailDtoAsync(
        TaskItem task,
        IUnitOfWork unitOfWork,
        CancellationToken cancellationToken)
    {
        var userRepo = unitOfWork.Repository<User>();

        var assignee = await (UserRefHelper.ToUserRefAsync(userRepo, task.AssigneeId, cancellationToken)).ConfigureAwait(false);
        var creator = await (UserRefHelper.ToUserRefAsync(userRepo, task.CreatorId, cancellationToken)).ConfigureAwait(false);
        var createdBy = await (UserRefHelper.ToUserRefAsync(userRepo, task.CreatedBy, cancellationToken)).ConfigureAwait(false);
        var updatedBy = await (UserRefHelper.ToUserRefAsync(userRepo, task.UpdatedBy, cancellationToken)).ConfigureAwait(false);

        var criterionDtos = (task.AcceptanceCriteria ?? [])
            .OrderBy(c => c.SortOrder)
            .Select(c => new TaskAcceptanceCriterionDto(
                Id: c.Id.ToString(),
                Description: c.Description,
                Completed: c.Completed))
            .ToList();

        var linkedItemDtos = (task.LinkedItems ?? [])
            .Select(l => new TaskLinkedItemDto(
                Id: l.Id.ToString(),
                Type: l.LinkedType,
                EntityId: l.LinkedEntityId.ToString(),
                EntityTitle: string.Empty,
                LinkedBy: l.LinkedBy.ToString(),
                LinkedAt: l.LinkedAt.ToDateTimeOffset()))
            .ToList();

        var timeLogDtos = new List<TaskTimeLogDto>();
        foreach (var log in (task.TimeLogs ?? []).OrderByDescending(l => l.LogDate))
        {
            var logUser = await (UserRefHelper.ToUserRefAsync(userRepo, log.UserId, cancellationToken)).ConfigureAwait(false);
            timeLogDtos.Add(new TaskTimeLogDto(
                Id: log.Id.ToString(),
                TaskId: task.Id.ToString(),
                User: logUser,
                Date: log.LogDate.ToString("yyyy-MM-dd"),
                Hours: log.Hours,
                Description: log.Description,
                CreatedAt: log.CreatedAt.ToDateTimeOffset()));
        }

        return new TaskItemDetailDto(
            Id: task.Id.ToString(),
            TenantId: task.TenantId.ToString(),
            ProjectId: task.ProjectId?.ToString(),
            Title: task.Title,
            Description: task.Description,
            Status: task.Status,
            Priority: task.Priority,
            Assignee: assignee,
            Creator: creator,
            EstimatedHours: task.EstimatedHours,
            ActualHours: task.ActualHours,
            AcceptanceCriteria: criterionDtos,
            LinkedItems: linkedItemDtos,
            TimeLogs: timeLogDtos,
            CreatedBy: createdBy,
            UpdatedBy: updatedBy,
            DueDate: task.DueDate,
            CategoryId: task.CategoryId?.ToString(),
            CreatedAt: task.CreatedAt.ToDateTimeOffset(),
            UpdatedAt: task.UpdatedAt.ToDateTimeOffset(),
            CompletedAt: task.CompletedAt?.ToDateTimeOffset());
    }
}
