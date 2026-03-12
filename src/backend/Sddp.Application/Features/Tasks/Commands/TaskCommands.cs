using MediatR;
using Sddp.Abstractions.DTOs;
using Sddp.Abstractions.Enums;
using Sddp.Abstractions.Exceptions;
using Sddp.Abstractions.Interfaces;
using Sddp.Abstractions.ValueObjects;
using Sddp.Application.Requests;
using Sddp.Application.Utilities;
using Sddp.Domain.Entities;

namespace Sddp.Application.Features.Tasks.Commands;

/// <summary>
/// Create task
/// </summary>
public sealed record CreateTaskCommand(
    GlobalUniqueId TenantId,
    GlobalUniqueId? ProjectId,
    GlobalUniqueId UserId,
    CreateTaskItemDto Dto) : ICommand<TaskItemDetailDto?>, IAuditableRequest<TaskItemDetailDto?>
{
    public AuditLogRequest? AuditLog { get; set; }
    public AuditLogRequest? GetAuditLogRequest(TaskItemDetailDto? response) => AuditLog;
}

public sealed class CreateTaskCommandHandler : IRequestHandler<CreateTaskCommand, TaskItemDetailDto?>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly INotificationService _notificationService;

    public CreateTaskCommandHandler(IUnitOfWork unitOfWork, INotificationService notificationService)
    {
        _unitOfWork = unitOfWork;
        _notificationService = notificationService;
    }

    public async Task<TaskItemDetailDto?> Handle(CreateTaskCommand request, CancellationToken cancellationToken)
    {
        var taskRepo = _unitOfWork.Repository<TaskItem>();

        GlobalUniqueId? assigneeId = null;
        if (!string.IsNullOrEmpty(request.Dto.AssigneeId))
        {
            if (!GlobalUniqueId.TryParse(request.Dto.AssigneeId, out var parsedAssigneeId))
            {
                throw new SddpException("VALIDATION_ERROR", "Invalid assignee ID format");
            }
            assigneeId = parsedAssigneeId;
        }

        var task = new TaskItem(
            request.TenantId,
            request.ProjectId,
            request.Dto.Title,
            request.Dto.Description,
            request.Dto.Priority,
            request.UserId,
            assigneeId,
            request.Dto.EstimatedHours,
            request.Dto.DueDate,
            request.Dto.Status ?? TaskItemStatus.ToDo);

        if (!string.IsNullOrEmpty(request.Dto.CategoryId))
        {
            if (GlobalUniqueId.TryParse(request.Dto.CategoryId, out var parsedCategoryId))
            {
                task.SetCategory(parsedCategoryId);
            }
        }

        task.SetCreatedBy(request.UserId);

        await (taskRepo.AddAsync(task, cancellationToken)).ConfigureAwait(false);
        await (_unitOfWork.SaveChangesAsync(cancellationToken)).ConfigureAwait(false);

        request.AuditLog = new AuditLogRequest(
            ActorId: request.UserId,
            Action: "create",
            ResourceType: "task",
            ResourceId: task.Id,
            Payload: new { task.Title, task.Priority, task.Status },
            TenantId: request.TenantId,
            ProjectId: request.ProjectId);

        // Create task assignee notification (excluding self)
        if (assigneeId.HasValue && assigneeId.Value != request.UserId)
        {
            await (_notificationService.CreateNotificationAsync(
                request.TenantId,
                assigneeId.Value,
                request.UserId,
                "task_assigned",
                $"A new task was assigned: {task.Title}",
                string.Empty,
                "task",
                task.Id,
                cancellationToken)).ConfigureAwait(false);
        }

        return await (TaskMapping.MapToDetailDtoAsync(task, _unitOfWork, cancellationToken)).ConfigureAwait(false);
    }
}

/// <summary>
/// Update task
/// </summary>
public sealed record UpdateTaskCommand(
    GlobalUniqueId TenantId,
    GlobalUniqueId TaskId,
    GlobalUniqueId UserId,
    UpdateTaskItemDto Dto) : ICommand<TaskItemDetailDto?>, IAuditableRequest<TaskItemDetailDto?>
{
    public AuditLogRequest? AuditLog { get; set; }
    public AuditLogRequest? GetAuditLogRequest(TaskItemDetailDto? response) => AuditLog;
}

public sealed class UpdateTaskCommandHandler : IRequestHandler<UpdateTaskCommand, TaskItemDetailDto?>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly INotificationService _notificationService;

    public UpdateTaskCommandHandler(IUnitOfWork unitOfWork, INotificationService notificationService)
    {
        _unitOfWork = unitOfWork;
        _notificationService = notificationService;
    }

    public async Task<TaskItemDetailDto?> Handle(UpdateTaskCommand request, CancellationToken cancellationToken)
    {
        var taskRepo = _unitOfWork.Repository<TaskItem>();

        var task = await (taskRepo.GetByIdAsync(request.TaskId, cancellationToken)).ConfigureAwait(false);
        if (task is null || task.TenantId != request.TenantId || !task.IsActive)
        {
            return null;
        }

        var title = request.Dto.Title ?? task.Title;
        var description = request.Dto.Description ?? task.Description;
        var priority = request.Dto.Priority ?? task.Priority;
        var estimatedHours = request.Dto.EstimatedHours ?? task.EstimatedHours;

        // Used to detect assignee changes
        var previousAssigneeId = task.AssigneeId;

        task.Update(title, description, priority, estimatedHours);

        if (request.Dto.DueDate is not null)
        {
            task.SetDueDate(request.Dto.DueDate);
        }

        GlobalUniqueId? newAssigneeId = null;
        if (request.Dto.AssigneeId is not null)
        {
            if (string.IsNullOrEmpty(request.Dto.AssigneeId))
            {
                task.AssignTo(null);
            }
            else if (GlobalUniqueId.TryParse(request.Dto.AssigneeId, out var parsedAssigneeId))
            {
                newAssigneeId = parsedAssigneeId;
                task.AssignTo(parsedAssigneeId);
            }
            else
            {
                throw new SddpException("UPDATE_ERROR", "Invalid assignee ID format");
            }
        }

        if (request.Dto.CategoryId is not null)
        {
            if (string.IsNullOrEmpty(request.Dto.CategoryId))
            {
                task.SetCategory(null);
            }
            else if (GlobalUniqueId.TryParse(request.Dto.CategoryId, out var parsedCategoryId))
            {
                task.SetCategory(parsedCategoryId);
            }
        }

        task.SetUpdatedBy(request.UserId);

        await (taskRepo.UpdateAsync(task, cancellationToken)).ConfigureAwait(false);
        await (_unitOfWork.SaveChangesAsync(cancellationToken)).ConfigureAwait(false);

        request.AuditLog = new AuditLogRequest(
            ActorId: request.UserId,
            Action: "update",
            ResourceType: "task",
            ResourceId: request.TaskId,
            Payload: new { task.Title, task.Priority, task.Status },
            TenantId: request.TenantId,
            ProjectId: null);

        // Notify when the assignee changes and the new assignee is not the acting user
        if (newAssigneeId.HasValue
            && newAssigneeId.Value != previousAssigneeId
            && newAssigneeId.Value != request.UserId)
        {
            await (_notificationService.CreateNotificationAsync(
                request.TenantId,
                newAssigneeId.Value,
                request.UserId,
                "task_assigned",
                $"A new task was assigned: {task.Title}",
                string.Empty,
                "task",
                task.Id,
                cancellationToken)).ConfigureAwait(false);
        }

        return await (TaskMapping.MapToDetailDtoAsync(task, _unitOfWork, cancellationToken)).ConfigureAwait(false);
    }
}

/// <summary>
/// Change task status
/// </summary>
public sealed record UpdateTaskStatusCommand(
    GlobalUniqueId TenantId,
    GlobalUniqueId TaskId,
    GlobalUniqueId UserId,
    TaskItemStatus NewStatus) : ICommand<TaskItemDto?>, IAuditableRequest<TaskItemDto?>
{
    public AuditLogRequest? AuditLog { get; set; }
    public AuditLogRequest? GetAuditLogRequest(TaskItemDto? response) => AuditLog;
}

public sealed class UpdateTaskStatusCommandHandler : IRequestHandler<UpdateTaskStatusCommand, TaskItemDto?>
{
    private readonly IUnitOfWork _unitOfWork;

    public UpdateTaskStatusCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<TaskItemDto?> Handle(UpdateTaskStatusCommand request, CancellationToken cancellationToken)
    {
        var taskRepo = _unitOfWork.Repository<TaskItem>();

        var task = await (taskRepo.GetByIdAsync(request.TaskId, cancellationToken)).ConfigureAwait(false);
        if (task is null || task.TenantId != request.TenantId || !task.IsActive)
        {
            return null;
        }

        var transitionResult = task.TransitionTo(request.NewStatus);
        transitionResult.EnsureSuccess("TRANSITION_ERROR");
        task.SetUpdatedBy(request.UserId);

        await (taskRepo.UpdateAsync(task, cancellationToken)).ConfigureAwait(false);
        await (_unitOfWork.SaveChangesAsync(cancellationToken)).ConfigureAwait(false);

        var userRepo = _unitOfWork.Repository<User>();
        var assignee = await (UserRefHelper.ToUserRefAsync(userRepo, task.AssigneeId, cancellationToken)).ConfigureAwait(false);
        var linkedItemCount = task.LinkedItems?.Count ?? 0;

        request.AuditLog = new AuditLogRequest(
            ActorId: request.UserId,
            Action: "status",
            ResourceType: "task",
            ResourceId: request.TaskId,
            Payload: new { NewStatus = request.NewStatus.ToString() },
            TenantId: request.TenantId,
            ProjectId: null);

        return TaskMapping.MapToDto(task, assignee, linkedItemCount);
    }
}

/// <summary>
/// task worklog
/// </summary>
public sealed record AddTaskTimeLogCommand(
    GlobalUniqueId TenantId,
    GlobalUniqueId TaskId,
    GlobalUniqueId UserId,
    CreateTaskTimeLogDto Dto) : ICommand<TaskTimeLogDto?>, IAuditableRequest<TaskTimeLogDto?>
{
    public AuditLogRequest? AuditLog { get; set; }
    public AuditLogRequest? GetAuditLogRequest(TaskTimeLogDto? response) => AuditLog;
}

public sealed class AddTaskTimeLogCommandHandler : IRequestHandler<AddTaskTimeLogCommand, TaskTimeLogDto?>
{
    private readonly IUnitOfWork _unitOfWork;

    public AddTaskTimeLogCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<TaskTimeLogDto?> Handle(AddTaskTimeLogCommand request, CancellationToken cancellationToken)
    {
        var taskRepo = _unitOfWork.Repository<TaskItem>();

        var task = await (taskRepo.GetByIdAsync(request.TaskId, cancellationToken)).ConfigureAwait(false);
        if (task is null || task.TenantId != request.TenantId || !task.IsActive)
        {
            return null;
        }

        if (!DateOnly.TryParse(request.Dto.Date, out var logDate))
        {
            throw new SddpException("VALIDATION_ERROR", "Invalid date format. Use yyyy-MM-dd");
        }

        if (request.Dto.Hours <= 0)
        {
            throw new SddpException("VALIDATION_ERROR", "Hours must be greater than 0");
        }

        var timeLogRepo = _unitOfWork.Repository<TaskTimeLog>();

        var timeLog = new TaskTimeLog(request.TaskId, request.UserId, logDate, request.Dto.Hours, request.Dto.Description);
        await (timeLogRepo.AddAsync(timeLog, cancellationToken)).ConfigureAwait(false);

        task.AddActualHours(request.Dto.Hours);
        task.SetUpdatedBy(request.UserId);
        await (taskRepo.UpdateAsync(task, cancellationToken)).ConfigureAwait(false);

        await (_unitOfWork.SaveChangesAsync(cancellationToken)).ConfigureAwait(false);

        var userRepo = _unitOfWork.Repository<User>();
        var user = await (UserRefHelper.ToUserRefAsync(userRepo, request.UserId, cancellationToken)).ConfigureAwait(false);

        request.AuditLog = new AuditLogRequest(
            ActorId: request.UserId,
            Action: "add_time_log",
            ResourceType: "task",
            ResourceId: request.TaskId,
            Payload: new { request.Dto.Hours },
            TenantId: request.TenantId,
            ProjectId: null);

        return new TaskTimeLogDto(
            Id: timeLog.Id.ToString(),
            TaskId: request.TaskId.ToString(),
            User: user,
            Date: timeLog.LogDate.ToString("yyyy-MM-dd"),
            Hours: timeLog.Hours,
            Description: timeLog.Description,
            CreatedAt: timeLog.CreatedAt.ToDateTimeOffset());
    }
}

/// <summary>
/// task change ()
/// </summary>
public sealed record UpdateTaskPositionCommand(
    GlobalUniqueId TenantId,
    GlobalUniqueId TaskId,
    GlobalUniqueId UserId,
    UpdateTaskPositionDto Dto) : ICommand<TaskItemDto?>, IAuditableRequest<TaskItemDto?>
{
    public AuditLogRequest? AuditLog { get; set; }
    public AuditLogRequest? GetAuditLogRequest(TaskItemDto? response) => AuditLog;
}

public sealed class UpdateTaskPositionCommandHandler : IRequestHandler<UpdateTaskPositionCommand, TaskItemDto?>
{
    private readonly IUnitOfWork _unitOfWork;

    public UpdateTaskPositionCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<TaskItemDto?> Handle(UpdateTaskPositionCommand request, CancellationToken cancellationToken)
    {
        var taskRepo = _unitOfWork.Repository<TaskItem>();

        var task = await (taskRepo.GetByIdAsync(request.TaskId, cancellationToken)).ConfigureAwait(false);
        if (task is null || task.TenantId != request.TenantId || !task.IsActive)
        {
            return null;
        }

        var positionResult = task.UpdatePosition(request.Dto.NewStatus, request.Dto.NewPosition);
        positionResult.EnsureSuccess("POSITION_ERROR");
        task.SetUpdatedBy(request.UserId);

        await (taskRepo.UpdateAsync(task, cancellationToken)).ConfigureAwait(false);
        await (_unitOfWork.SaveChangesAsync(cancellationToken)).ConfigureAwait(false);

        var userRepo = _unitOfWork.Repository<User>();
        var assignee = await (UserRefHelper.ToUserRefAsync(userRepo, task.AssigneeId, cancellationToken)).ConfigureAwait(false);
        var linkedItemCount = task.LinkedItems?.Count ?? 0;

        request.AuditLog = new AuditLogRequest(
            ActorId: request.UserId,
            Action: "update_position",
            ResourceType: "task",
            ResourceId: request.TaskId,
            Payload: new { request.Dto.NewStatus, request.Dto.NewPosition },
            TenantId: request.TenantId,
            ProjectId: null);

        return TaskMapping.MapToDto(task, assignee, linkedItemCount);
    }
}

/// <summary>
/// task
/// </summary>
public sealed record AddTaskLinkedItemCommand(
    GlobalUniqueId TenantId,
    GlobalUniqueId TaskId,
    GlobalUniqueId UserId,
    CreateTaskLinkedItemDto Dto) : ICommand<TaskLinkedItemDto?>, IAuditableRequest<TaskLinkedItemDto?>
{
    public AuditLogRequest? AuditLog { get; set; }
    public AuditLogRequest? GetAuditLogRequest(TaskLinkedItemDto? response) => AuditLog;
}

public sealed class AddTaskLinkedItemCommandHandler : IRequestHandler<AddTaskLinkedItemCommand, TaskLinkedItemDto?>
{
    private readonly IUnitOfWork _unitOfWork;

    public AddTaskLinkedItemCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<TaskLinkedItemDto?> Handle(AddTaskLinkedItemCommand request, CancellationToken cancellationToken)
    {
        var taskRepo = _unitOfWork.Repository<TaskItem>();

        var task = await (taskRepo.GetByIdAsync(request.TaskId, cancellationToken)).ConfigureAwait(false);
        if (task is null || task.TenantId != request.TenantId || !task.IsActive)
        {
            return null;
        }

        if (!GlobalUniqueId.TryParse(request.Dto.LinkedEntityId, out var linkedEntityId))
        {
            throw new SddpException("VALIDATION_ERROR", "Invalid linked entity ID format");
        }

        var linkedItemRepo = _unitOfWork.Repository<TaskLinkedItem>();

        var linkedItem = new TaskLinkedItem(
            request.TaskId,
            request.Dto.LinkedType,
            linkedEntityId,
            request.UserId);

        await (linkedItemRepo.AddAsync(linkedItem, cancellationToken)).ConfigureAwait(false);
        task.SetUpdatedBy(request.UserId);
        await (taskRepo.UpdateAsync(task, cancellationToken)).ConfigureAwait(false);
        await (_unitOfWork.SaveChangesAsync(cancellationToken)).ConfigureAwait(false);

        request.AuditLog = new AuditLogRequest(
            ActorId: request.UserId,
            Action: "add_linked_item",
            ResourceType: "task",
            ResourceId: request.TaskId,
            Payload: new { request.Dto.LinkedType, request.Dto.LinkedEntityId },
            TenantId: request.TenantId,
            ProjectId: null);

        return new TaskLinkedItemDto(
            Id: linkedItem.Id.ToString(),
            Type: linkedItem.LinkedType,
            EntityId: linkedItem.LinkedEntityId.ToString(),
            EntityTitle: "",
            LinkedBy: request.UserId.ToString(),
            LinkedAt: linkedItem.LinkedAt.ToDateTimeOffset());
    }
}

/// <summary>
/// task
/// </summary>
public sealed record RemoveTaskLinkedItemCommand(
    GlobalUniqueId TenantId,
    GlobalUniqueId TaskId,
    GlobalUniqueId LinkedItemId) : ICommand<bool>, IAuditableRequest<bool>
{
    public AuditLogRequest? AuditLog { get; set; }
    public AuditLogRequest? GetAuditLogRequest(bool response) => response ? AuditLog : null;
}

public sealed class RemoveTaskLinkedItemCommandHandler : IRequestHandler<RemoveTaskLinkedItemCommand, bool>
{
    private readonly IUnitOfWork _unitOfWork;

    public RemoveTaskLinkedItemCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<bool> Handle(RemoveTaskLinkedItemCommand request, CancellationToken cancellationToken)
    {
        var taskRepo = _unitOfWork.Repository<TaskItem>();

        var task = await (taskRepo.GetByIdAsync(request.TaskId, cancellationToken)).ConfigureAwait(false);
        if (task is null || task.TenantId != request.TenantId || !task.IsActive)
        {
            return false;
        }

        var linkedItemRepo = _unitOfWork.Repository<TaskLinkedItem>();
        var linkedItem = await (linkedItemRepo.GetByIdAsync(request.LinkedItemId, cancellationToken)).ConfigureAwait(false);
        if (linkedItem is null || linkedItem.TaskId != request.TaskId)
        {
            return false;
        }

        await (linkedItemRepo.DeleteAsync(linkedItem, cancellationToken)).ConfigureAwait(false);
        await (_unitOfWork.SaveChangesAsync(cancellationToken)).ConfigureAwait(false);

        request.AuditLog = new AuditLogRequest(
            ActorId: null,
            Action: "remove_linked_item",
            ResourceType: "task",
            ResourceId: request.TaskId,
            Payload: new { LinkedItemId = request.LinkedItemId.ToString() },
            TenantId: request.TenantId,
            ProjectId: null);

        return true;
    }
}

/// <summary>
/// task delete (soft delete)
/// </summary>
public sealed record DeleteTaskCommand(
    GlobalUniqueId TenantId,
    GlobalUniqueId TaskId,
    GlobalUniqueId UserId,
    GlobalUniqueId? ProjectId = null,
    bool IsAdmin = false) : ICommand<bool>, IAuditableRequest<bool>
{
    public AuditLogRequest? AuditLog { get; set; }
    public AuditLogRequest? GetAuditLogRequest(bool response) => response ? AuditLog : null;
}

public sealed class DeleteTaskCommandHandler : IRequestHandler<DeleteTaskCommand, bool>
{
    private readonly IUnitOfWork _unitOfWork;

    public DeleteTaskCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<bool> Handle(DeleteTaskCommand request, CancellationToken cancellationToken)
    {
        var taskRepo = _unitOfWork.Repository<TaskItem>();

        var task = await (taskRepo.GetByIdAsync(request.TaskId, cancellationToken)).ConfigureAwait(false);
        if (task is null || task.TenantId != request.TenantId || !task.IsActive)
        {
            return false;
        }

        // ProjectId, task project
        if (request.ProjectId is GlobalUniqueId requestedProjectId && task.ProjectId != requestedProjectId)
        {
            return false;
        }

        // X-Project-Id project task user.
        if (task.ProjectId is GlobalUniqueId taskProjectId && !request.IsAdmin)
        {
            var projectMemberRepo = _unitOfWork.Repository<ProjectMember>();
            var isProjectMember = await (projectMemberRepo.CountAsync(
                m => m.ProjectId == taskProjectId
                    && m.UserId == request.UserId
                    && m.IsActive,
                cancellationToken)) .ConfigureAwait(false)> 0;

            if (!isProjectMember)
            {
                return false;
            }
        }

        task.Deactivate();
        await (taskRepo.UpdateAsync(task, cancellationToken)).ConfigureAwait(false);
        await (_unitOfWork.SaveChangesAsync(cancellationToken)).ConfigureAwait(false);

        request.AuditLog = new AuditLogRequest(
            ActorId: request.UserId,
            Action: AuditLog.Actions.Deleted,
            ResourceType: "task",
            ResourceId: task.Id,
            Payload: new { task.Title, Status = task.Status.ToString() },
            TenantId: request.TenantId,
            ProjectId: task.ProjectId);

        return true;
    }
}
