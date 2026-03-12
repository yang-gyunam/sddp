using FluentValidation;
using Sddp.Abstractions.ValueObjects;
using Sddp.Application.Features.Requirements.Commands;

namespace Sddp.Application.Features.Requirements.Validators;

public sealed class CreateRequirementCommandValidator : AbstractValidator<CreateRequirementCommand>
{
    public CreateRequirementCommandValidator()
    {
        RuleFor(x => x.TenantId).Must(NotEmptyId).WithMessage("TenantId is required.");
        RuleFor(x => x.ProjectId).Must(NotEmptyId).WithMessage("ProjectId is required.");
        RuleFor(x => x.UserId).Must(NotEmptyId).WithMessage("UserId is required.");
        RuleFor(x => x.Dto.Code).NotEmpty().WithMessage("Code is required.");
        RuleFor(x => x.Dto.Title).NotEmpty().WithMessage("Title is required.");
        RuleFor(x => x.Dto.Priority).IsInEnum().WithMessage("Priority is invalid.");
        RuleFor(x => x.Dto.ParentId)
            .Must(BeValidIdOrEmpty)
            .WithMessage("ParentId must be a valid ID.");
    }

    private static bool NotEmptyId(GlobalUniqueId id) => !id.IsEmpty;
    private static bool BeValidIdOrEmpty(string? value) =>
        string.IsNullOrWhiteSpace(value) || GlobalUniqueId.TryParse(value, out _);
}

public sealed class UpdateRequirementCommandValidator : AbstractValidator<UpdateRequirementCommand>
{
    public UpdateRequirementCommandValidator()
    {
        RuleFor(x => x.TenantId).Must(NotEmptyId).WithMessage("TenantId is required.");
        RuleFor(x => x.ProjectId).Must(NotEmptyId).WithMessage("ProjectId is required.");
        RuleFor(x => x.RequirementId).Must(NotEmptyId).WithMessage("RequirementId is required.");
        RuleFor(x => x.UserId).Must(NotEmptyId).WithMessage("UserId is required.");
        RuleFor(x => x.Dto.Title).NotEmpty().WithMessage("Title is required.");
    }

    private static bool NotEmptyId(GlobalUniqueId id) => !id.IsEmpty;
}

public sealed class TransitionRequirementStatusCommandValidator : AbstractValidator<TransitionRequirementStatusCommand>
{
    public TransitionRequirementStatusCommandValidator()
    {
        RuleFor(x => x.TenantId).Must(NotEmptyId).WithMessage("TenantId is required.");
        RuleFor(x => x.ProjectId).Must(NotEmptyId).WithMessage("ProjectId is required.");
        RuleFor(x => x.RequirementId).Must(NotEmptyId).WithMessage("RequirementId is required.");
        RuleFor(x => x.UserId).Must(NotEmptyId).WithMessage("UserId is required.");
        RuleFor(x => x.NewStatus).IsInEnum().WithMessage("Status is invalid.");
    }

    private static bool NotEmptyId(GlobalUniqueId id) => !id.IsEmpty;
}

public sealed class DeleteRequirementCommandValidator : AbstractValidator<DeleteRequirementCommand>
{
    public DeleteRequirementCommandValidator()
    {
        RuleFor(x => x.TenantId).Must(NotEmptyId).WithMessage("TenantId is required.");
        RuleFor(x => x.ProjectId).Must(NotEmptyId).WithMessage("ProjectId is required.");
        RuleFor(x => x.RequirementId).Must(NotEmptyId).WithMessage("RequirementId is required.");
    }

    private static bool NotEmptyId(GlobalUniqueId id) => !id.IsEmpty;
}

public sealed class LinkRequirementConversationCommandValidator : AbstractValidator<LinkRequirementConversationCommand>
{
    public LinkRequirementConversationCommandValidator()
    {
        RuleFor(x => x.TenantId).Must(NotEmptyId).WithMessage("TenantId is required.");
        RuleFor(x => x.ProjectId).Must(NotEmptyId).WithMessage("ProjectId is required.");
        RuleFor(x => x.RequirementId).Must(NotEmptyId).WithMessage("RequirementId is required.");
        RuleFor(x => x.ConversationId).Must(NotEmptyId).WithMessage("ConversationId is required.");
    }

    private static bool NotEmptyId(GlobalUniqueId id) => !id.IsEmpty;
}

public sealed class UnlinkRequirementConversationCommandValidator : AbstractValidator<UnlinkRequirementConversationCommand>
{
    public UnlinkRequirementConversationCommandValidator()
    {
        RuleFor(x => x.TenantId).Must(NotEmptyId).WithMessage("TenantId is required.");
        RuleFor(x => x.ProjectId).Must(NotEmptyId).WithMessage("ProjectId is required.");
        RuleFor(x => x.RequirementId).Must(NotEmptyId).WithMessage("RequirementId is required.");
    }

    private static bool NotEmptyId(GlobalUniqueId id) => !id.IsEmpty;
}
