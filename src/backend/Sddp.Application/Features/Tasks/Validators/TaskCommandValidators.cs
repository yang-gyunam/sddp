using FluentValidation;
using Sddp.Abstractions.DTOs;
using Sddp.Abstractions.Enums;
using Sddp.Abstractions.ValueObjects;
using Sddp.Application.Features.Tasks.Commands;

namespace Sddp.Application.Features.Tasks.Validators;

public sealed class CreateTaskCommandValidator : AbstractValidator<CreateTaskCommand>
{
    public CreateTaskCommandValidator()
    {
        RuleFor(x => x.TenantId).Must(NotEmptyId).WithMessage("TenantId is required.");
        RuleFor(x => x.ProjectId).Must(id => id is null || !id.Value.IsEmpty).WithMessage("ProjectId must be null or a valid ID.");
        RuleFor(x => x.UserId).Must(NotEmptyId).WithMessage("UserId is required.");
        RuleFor(x => x.Dto.Title).NotEmpty().WithMessage("Title is required.");
        RuleFor(x => x.Dto.Priority).IsInEnum().WithMessage("Priority is invalid.");
        RuleFor(x => x.Dto.Status)
            .Must(status => status is null || Enum.IsDefined(typeof(TaskItemStatus), status))
            .WithMessage("Status is invalid.");
        RuleFor(x => x.Dto.AssigneeId)
            .Must(BeValidIdOrEmpty)
            .WithMessage("AssigneeId must be a valid ID.");
        RuleFor(x => x.Dto.EstimatedHours)
            .GreaterThanOrEqualTo(0)
            .WithMessage("Estimated hours must be >= 0.");
    }

    private static bool NotEmptyId(GlobalUniqueId id) => !id.IsEmpty;
    private static bool BeValidIdOrEmpty(string? value) =>
        string.IsNullOrWhiteSpace(value) || GlobalUniqueId.TryParse(value, out _);
}

public sealed class UpdateTaskCommandValidator : AbstractValidator<UpdateTaskCommand>
{
    public UpdateTaskCommandValidator()
    {
        RuleFor(x => x.TenantId).Must(NotEmptyId).WithMessage("TenantId is required.");
        RuleFor(x => x.TaskId).Must(NotEmptyId).WithMessage("TaskId is required.");
        RuleFor(x => x.UserId).Must(NotEmptyId).WithMessage("UserId is required.");
        RuleFor(x => x.Dto.Priority)
            .Must(priority => priority is null || Enum.IsDefined(typeof(TaskItemPriority), priority))
            .WithMessage("Priority is invalid.");
        RuleFor(x => x.Dto.AssigneeId)
            .Must(BeValidIdOrEmpty)
            .WithMessage("AssigneeId must be a valid ID.");
        RuleFor(x => x.Dto.EstimatedHours)
            .Must(hours => hours is null || hours >= 0)
            .WithMessage("Estimated hours must be >= 0.");
    }

    private static bool NotEmptyId(GlobalUniqueId id) => !id.IsEmpty;
    private static bool BeValidIdOrEmpty(string? value) =>
        string.IsNullOrWhiteSpace(value) || GlobalUniqueId.TryParse(value, out _);
}

public sealed class UpdateTaskStatusCommandValidator : AbstractValidator<UpdateTaskStatusCommand>
{
    public UpdateTaskStatusCommandValidator()
    {
        RuleFor(x => x.TenantId).Must(NotEmptyId).WithMessage("TenantId is required.");
        RuleFor(x => x.TaskId).Must(NotEmptyId).WithMessage("TaskId is required.");
        RuleFor(x => x.UserId).Must(NotEmptyId).WithMessage("UserId is required.");
        RuleFor(x => x.NewStatus).IsInEnum().WithMessage("Status is invalid.");
    }

    private static bool NotEmptyId(GlobalUniqueId id) => !id.IsEmpty;
}

public sealed class AddTaskTimeLogCommandValidator : AbstractValidator<AddTaskTimeLogCommand>
{
    public AddTaskTimeLogCommandValidator()
    {
        RuleFor(x => x.TenantId).Must(NotEmptyId).WithMessage("TenantId is required.");
        RuleFor(x => x.TaskId).Must(NotEmptyId).WithMessage("TaskId is required.");
        RuleFor(x => x.UserId).Must(NotEmptyId).WithMessage("UserId is required.");
        RuleFor(x => x.Dto.Date).NotEmpty().WithMessage("Date is required.");
        RuleFor(x => x.Dto.Hours).GreaterThan(0).WithMessage("Hours must be greater than 0.");
    }

    private static bool NotEmptyId(GlobalUniqueId id) => !id.IsEmpty;
}

public sealed class UpdateTaskPositionCommandValidator : AbstractValidator<UpdateTaskPositionCommand>
{
    public UpdateTaskPositionCommandValidator()
    {
        RuleFor(x => x.TenantId).Must(NotEmptyId).WithMessage("TenantId is required.");
        RuleFor(x => x.TaskId).Must(NotEmptyId).WithMessage("TaskId is required.");
        RuleFor(x => x.UserId).Must(NotEmptyId).WithMessage("UserId is required.");
        RuleFor(x => x.Dto.NewStatus).IsInEnum().WithMessage("Status is invalid.");
        RuleFor(x => x.Dto.NewPosition).GreaterThanOrEqualTo(0).WithMessage("NewPosition must be >= 0.");
    }

    private static bool NotEmptyId(GlobalUniqueId id) => !id.IsEmpty;
}

public sealed class AddTaskLinkedItemCommandValidator : AbstractValidator<AddTaskLinkedItemCommand>
{
    private static readonly HashSet<string> AllowedLinkedTypes =
    [
        "conversation",
        "requirement",
        "spec",
        "artifact",
        "glossary"
    ];

    public AddTaskLinkedItemCommandValidator()
    {
        RuleFor(x => x.TenantId).Must(NotEmptyId).WithMessage("TenantId is required.");
        RuleFor(x => x.TaskId).Must(NotEmptyId).WithMessage("TaskId is required.");
        RuleFor(x => x.UserId).Must(NotEmptyId).WithMessage("UserId is required.");
        RuleFor(x => x.Dto.LinkedType)
            .NotEmpty().WithMessage("LinkedType is required.")
            .Must(IsAllowedLinkedType)
            .WithMessage("LinkedType must be one of: conversation, requirement, spec, artifact, glossary.");
        RuleFor(x => x.Dto.LinkedEntityId).NotEmpty().WithMessage("LinkedEntityId is required.");
    }

    private static bool NotEmptyId(GlobalUniqueId id) => !id.IsEmpty;
    private static bool IsAllowedLinkedType(string linkedType) => AllowedLinkedTypes.Contains(linkedType);
}

public sealed class RemoveTaskLinkedItemCommandValidator : AbstractValidator<RemoveTaskLinkedItemCommand>
{
    public RemoveTaskLinkedItemCommandValidator()
    {
        RuleFor(x => x.TenantId).Must(NotEmptyId).WithMessage("TenantId is required.");
        RuleFor(x => x.TaskId).Must(NotEmptyId).WithMessage("TaskId is required.");
        RuleFor(x => x.LinkedItemId).Must(NotEmptyId).WithMessage("LinkedItemId is required.");
    }

    private static bool NotEmptyId(GlobalUniqueId id) => !id.IsEmpty;
}
