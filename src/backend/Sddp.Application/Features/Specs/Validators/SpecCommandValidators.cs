using FluentValidation;
using Sddp.Abstractions.Enums;
using Sddp.Abstractions.ValueObjects;
using Sddp.Application.Features.Specs.Commands;

namespace Sddp.Application.Features.Specs.Validators;

public sealed class CreateSpecCommandValidator : AbstractValidator<CreateSpecCommand>
{
    public CreateSpecCommandValidator()
    {
        RuleFor(x => x.TenantId).Must(NotEmptyId).WithMessage("TenantId is required.");
        RuleFor(x => x.ProjectId).Must(NotEmptyId).WithMessage("ProjectId is required.");
        RuleFor(x => x.UserId).Must(NotEmptyId).WithMessage("UserId is required.");
        RuleFor(x => x.Dto.Code).NotEmpty().WithMessage("Code is required.");
        RuleFor(x => x.Dto.Title).NotEmpty().WithMessage("Title is required.");
        RuleFor(x => x.Dto.RequirementId)
            .Must(BeValidIdOrEmpty)
            .WithMessage("RequirementId must be a valid ID.");
        RuleFor(x => x.Dto.BornFromConversationId)
            .Must(BeValidIdOrEmpty)
            .WithMessage("BornFromConversationId must be a valid ID.");
    }

    private static bool NotEmptyId(GlobalUniqueId id) => !id.IsEmpty;
    private static bool BeValidIdOrEmpty(string? value) =>
        string.IsNullOrWhiteSpace(value) || GlobalUniqueId.TryParse(value, out _);
}

public sealed class UpdateSpecCommandValidator : AbstractValidator<UpdateSpecCommand>
{
    public UpdateSpecCommandValidator()
    {
        RuleFor(x => x.TenantId).Must(NotEmptyId).WithMessage("TenantId is required.");
        RuleFor(x => x.ProjectId).Must(NotEmptyId).WithMessage("ProjectId is required.");
        RuleFor(x => x.SpecId).Must(NotEmptyId).WithMessage("SpecId is required.");
        RuleFor(x => x.UserId).Must(NotEmptyId).WithMessage("UserId is required.");
        RuleFor(x => x.Dto.Title).NotEmpty().WithMessage("Title is required.");
    }

    private static bool NotEmptyId(GlobalUniqueId id) => !id.IsEmpty;
}

public sealed class DeleteSpecCommandValidator : AbstractValidator<DeleteSpecCommand>
{
    public DeleteSpecCommandValidator()
    {
        RuleFor(x => x.TenantId).Must(NotEmptyId).WithMessage("TenantId is required.");
        RuleFor(x => x.ProjectId).Must(NotEmptyId).WithMessage("ProjectId is required.");
        RuleFor(x => x.SpecId).Must(NotEmptyId).WithMessage("SpecId is required.");
    }

    private static bool NotEmptyId(GlobalUniqueId id) => !id.IsEmpty;
}

public sealed class TransitionSpecStatusCommandValidator : AbstractValidator<TransitionSpecStatusCommand>
{
    public TransitionSpecStatusCommandValidator()
    {
        RuleFor(x => x.TenantId).Must(NotEmptyId).WithMessage("TenantId is required.");
        RuleFor(x => x.ProjectId).Must(NotEmptyId).WithMessage("ProjectId is required.");
        RuleFor(x => x.SpecId).Must(NotEmptyId).WithMessage("SpecId is required.");
        RuleFor(x => x.UserId).Must(NotEmptyId).WithMessage("UserId is required.");
    }

    private static bool NotEmptyId(GlobalUniqueId id) => !id.IsEmpty;
}

public sealed class SubmitSpecForReviewCommandValidator : AbstractValidator<SubmitSpecForReviewCommand>
{
    public SubmitSpecForReviewCommandValidator()
    {
        RuleFor(x => x.TenantId).Must(NotEmptyId).WithMessage("TenantId is required.");
        RuleFor(x => x.ProjectId).Must(NotEmptyId).WithMessage("ProjectId is required.");
        RuleFor(x => x.SpecId).Must(NotEmptyId).WithMessage("SpecId is required.");
        RuleFor(x => x.UserId).Must(NotEmptyId).WithMessage("UserId is required.");
    }

    private static bool NotEmptyId(GlobalUniqueId id) => !id.IsEmpty;
}

public sealed class ApproveSpecCommandValidator : AbstractValidator<ApproveSpecCommand>
{
    public ApproveSpecCommandValidator()
    {
        RuleFor(x => x.TenantId).Must(NotEmptyId).WithMessage("TenantId is required.");
        RuleFor(x => x.ProjectId).Must(NotEmptyId).WithMessage("ProjectId is required.");
        RuleFor(x => x.SpecId).Must(NotEmptyId).WithMessage("SpecId is required.");
        RuleFor(x => x.UserId).Must(NotEmptyId).WithMessage("UserId is required.");
    }

    private static bool NotEmptyId(GlobalUniqueId id) => !id.IsEmpty;
}

public sealed class RejectSpecCommandValidator : AbstractValidator<RejectSpecCommand>
{
    public RejectSpecCommandValidator()
    {
        RuleFor(x => x.TenantId).Must(NotEmptyId).WithMessage("TenantId is required.");
        RuleFor(x => x.ProjectId).Must(NotEmptyId).WithMessage("ProjectId is required.");
        RuleFor(x => x.SpecId).Must(NotEmptyId).WithMessage("SpecId is required.");
        RuleFor(x => x.UserId).Must(NotEmptyId).WithMessage("UserId is required.");
    }

    private static bool NotEmptyId(GlobalUniqueId id) => !id.IsEmpty;
}

public sealed class LockSpecCommandValidator : AbstractValidator<LockSpecCommand>
{
    public LockSpecCommandValidator()
    {
        RuleFor(x => x.TenantId).Must(NotEmptyId).WithMessage("TenantId is required.");
        RuleFor(x => x.ProjectId).Must(NotEmptyId).WithMessage("ProjectId is required.");
        RuleFor(x => x.SpecId).Must(NotEmptyId).WithMessage("SpecId is required.");
        RuleFor(x => x.UserId).Must(NotEmptyId).WithMessage("UserId is required.");
    }

    private static bool NotEmptyId(GlobalUniqueId id) => !id.IsEmpty;
}

public sealed class LinkSpecRequirementCommandValidator : AbstractValidator<LinkSpecRequirementCommand>
{
    public LinkSpecRequirementCommandValidator()
    {
        RuleFor(x => x.TenantId).Must(NotEmptyId).WithMessage("TenantId is required.");
        RuleFor(x => x.ProjectId).Must(NotEmptyId).WithMessage("ProjectId is required.");
        RuleFor(x => x.SpecId).Must(NotEmptyId).WithMessage("SpecId is required.");
        RuleFor(x => x.RequirementId).Must(NotEmptyId).WithMessage("RequirementId is required.");
        RuleFor(x => x.UserId).Must(NotEmptyId).WithMessage("UserId is required.");
    }

    private static bool NotEmptyId(GlobalUniqueId id) => !id.IsEmpty;
}

public sealed class UnlinkSpecRequirementCommandValidator : AbstractValidator<UnlinkSpecRequirementCommand>
{
    public UnlinkSpecRequirementCommandValidator()
    {
        RuleFor(x => x.TenantId).Must(NotEmptyId).WithMessage("TenantId is required.");
        RuleFor(x => x.ProjectId).Must(NotEmptyId).WithMessage("ProjectId is required.");
        RuleFor(x => x.SpecId).Must(NotEmptyId).WithMessage("SpecId is required.");
        RuleFor(x => x.UserId).Must(NotEmptyId).WithMessage("UserId is required.");
    }

    private static bool NotEmptyId(GlobalUniqueId id) => !id.IsEmpty;
}

public sealed class SubmitSpecSignOffCommandValidator : AbstractValidator<SubmitSpecSignOffCommand>
{
    public SubmitSpecSignOffCommandValidator()
    {
        RuleFor(x => x.TenantId).Must(NotEmptyId).WithMessage("TenantId is required.");
        RuleFor(x => x.ProjectId).Must(NotEmptyId).WithMessage("ProjectId is required.");
        RuleFor(x => x.SpecId).Must(NotEmptyId).WithMessage("SpecId is required.");
        RuleFor(x => x.UserId).Must(NotEmptyId).WithMessage("UserId is required.");
        RuleFor(x => x.Dto.Decision)
            .Must(decision => decision != SignOffDecision.Pending)
            .WithMessage("Sign-off decision is required.");
    }

    private static bool NotEmptyId(GlobalUniqueId id) => !id.IsEmpty;
}

public sealed class CreateSpecNewVersionCommandValidator : AbstractValidator<CreateSpecNewVersionCommand>
{
    public CreateSpecNewVersionCommandValidator()
    {
        RuleFor(x => x.TenantId).Must(NotEmptyId).WithMessage("TenantId is required.");
        RuleFor(x => x.ProjectId).Must(NotEmptyId).WithMessage("ProjectId is required.");
        RuleFor(x => x.SpecId).Must(NotEmptyId).WithMessage("SpecId is required.");
        RuleFor(x => x.UserId).Must(NotEmptyId).WithMessage("UserId is required.");
    }

    private static bool NotEmptyId(GlobalUniqueId id) => !id.IsEmpty;
}
