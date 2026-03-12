using FluentValidation;
using Sddp.Abstractions.DTOs;
using Sddp.Abstractions.ValueObjects;
using Sddp.Application.Features.Effort.Commands;

namespace Sddp.Application.Features.Effort.Validators;

public sealed class UpsertEffortAllocationCommandValidator : AbstractValidator<UpsertEffortAllocationCommand>
{
    public UpsertEffortAllocationCommandValidator()
    {
        RuleFor(x => x.TenantId).Must(NotEmptyId).WithMessage("TenantId is required.");
        RuleFor(x => x.ProjectId).Must(NotEmptyId).WithMessage("ProjectId is required.");
        RuleFor(x => x.UserId).Must(NotEmptyId).WithMessage("UserId is required.");
        RuleFor(x => x.Request.UserId).NotEmpty().WithMessage("UserId is required.");
        RuleFor(x => x.Request.Date)
            .NotEmpty().WithMessage("Date is required.")
            .Must(BeValidDate).WithMessage("Date must be a valid yyyy-MM-dd value.");
    }

    private static bool NotEmptyId(GlobalUniqueId id) => !id.IsEmpty;
    private static bool BeValidDate(string value) => DateOnly.TryParse(value, out _);
}

public sealed class BulkUpsertEffortAllocationsCommandValidator : AbstractValidator<BulkUpsertEffortAllocationsCommand>
{
    public BulkUpsertEffortAllocationsCommandValidator()
    {
        RuleFor(x => x.TenantId).Must(NotEmptyId).WithMessage("TenantId is required.");
        RuleFor(x => x.ProjectId).Must(NotEmptyId).WithMessage("ProjectId is required.");
        RuleFor(x => x.UserId).Must(NotEmptyId).WithMessage("UserId is required.");
        RuleForEach(x => x.Request.Allocations).ChildRules(allocation =>
        {
            allocation.RuleFor(a => a.UserId).NotEmpty().WithMessage("UserId is required.");
            allocation.RuleFor(a => a.Date)
                .NotEmpty().WithMessage("Date is required.")
                .Must(BeValidDate).WithMessage("Date must be a valid yyyy-MM-dd value.");
        });
    }

    private static bool NotEmptyId(GlobalUniqueId id) => !id.IsEmpty;
    private static bool BeValidDate(string value) => DateOnly.TryParse(value, out _);
}

public sealed class DeleteEffortAllocationCommandValidator : AbstractValidator<DeleteEffortAllocationCommand>
{
    public DeleteEffortAllocationCommandValidator()
    {
        RuleFor(x => x.TenantId).Must(NotEmptyId).WithMessage("TenantId is required.");
        RuleFor(x => x.ProjectId).Must(NotEmptyId).WithMessage("ProjectId is required.");
        RuleFor(x => x.AllocationId).Must(NotEmptyId).WithMessage("AllocationId is required.");
    }

    private static bool NotEmptyId(GlobalUniqueId id) => !id.IsEmpty;
}

public sealed class CreateWorklogCommandValidator : AbstractValidator<CreateWorklogCommand>
{
    public CreateWorklogCommandValidator()
    {
        RuleFor(x => x.TenantId).Must(NotEmptyId).WithMessage("TenantId is required.");
        RuleFor(x => x.ProjectId).Must(NotEmptyId).WithMessage("ProjectId is required.");
        RuleFor(x => x.Request.UserId).NotEmpty().WithMessage("UserId is required.");
        RuleFor(x => x.Request.Date)
            .NotEmpty().WithMessage("Date is required.")
            .Must(BeValidDate).WithMessage("Date must be a valid yyyy-MM-dd value.");
    }

    private static bool NotEmptyId(GlobalUniqueId id) => !id.IsEmpty;
    private static bool BeValidDate(string value) => DateOnly.TryParse(value, out _);
}

public sealed class UpdateWorklogCommandValidator : AbstractValidator<UpdateWorklogCommand>
{
    public UpdateWorklogCommandValidator()
    {
        RuleFor(x => x.TenantId).Must(NotEmptyId).WithMessage("TenantId is required.");
        RuleFor(x => x.ProjectId).Must(NotEmptyId).WithMessage("ProjectId is required.");
        RuleFor(x => x.WorklogId).Must(NotEmptyId).WithMessage("WorklogId is required.");
    }

    private static bool NotEmptyId(GlobalUniqueId id) => !id.IsEmpty;
}

public sealed class DeleteWorklogCommandValidator : AbstractValidator<DeleteWorklogCommand>
{
    public DeleteWorklogCommandValidator()
    {
        RuleFor(x => x.TenantId).Must(NotEmptyId).WithMessage("TenantId is required.");
        RuleFor(x => x.ProjectId).Must(NotEmptyId).WithMessage("ProjectId is required.");
        RuleFor(x => x.WorklogId).Must(NotEmptyId).WithMessage("WorklogId is required.");
    }

    private static bool NotEmptyId(GlobalUniqueId id) => !id.IsEmpty;
}

public sealed class SetWorkingDayCommandValidator : AbstractValidator<SetWorkingDayCommand>
{
    public SetWorkingDayCommandValidator()
    {
        RuleFor(x => x.TenantId).Must(NotEmptyId).WithMessage("TenantId is required.");
        RuleFor(x => x.ProjectId).Must(NotEmptyId).WithMessage("ProjectId is required.");
        RuleFor(x => x.UserId).Must(NotEmptyId).WithMessage("UserId is required.");
        RuleFor(x => x.Request.Date)
            .NotEmpty().WithMessage("Date is required.")
            .Must(BeValidDate).WithMessage("Date must be a valid yyyy-MM-dd value.");
    }

    private static bool NotEmptyId(GlobalUniqueId id) => !id.IsEmpty;
    private static bool BeValidDate(string value) => DateOnly.TryParse(value, out _);
}

public sealed class BulkSetWorkingDaysCommandValidator : AbstractValidator<BulkSetWorkingDaysCommand>
{
    public BulkSetWorkingDaysCommandValidator()
    {
        RuleFor(x => x.TenantId).Must(NotEmptyId).WithMessage("TenantId is required.");
        RuleFor(x => x.ProjectId).Must(NotEmptyId).WithMessage("ProjectId is required.");
        RuleFor(x => x.UserId).Must(NotEmptyId).WithMessage("UserId is required.");
        RuleForEach(x => x.Request.WorkingDays).ChildRules(workingDay =>
        {
            workingDay.RuleFor(wd => wd.Date)
                .NotEmpty().WithMessage("Date is required.")
                .Must(BeValidDate).WithMessage("Date must be a valid yyyy-MM-dd value.");
        });
    }

    private static bool NotEmptyId(GlobalUniqueId id) => !id.IsEmpty;
    private static bool BeValidDate(string value) => DateOnly.TryParse(value, out _);
}
