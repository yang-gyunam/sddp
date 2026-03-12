using FluentValidation;
using Sddp.Abstractions.ValueObjects;
using Sddp.Application.Features.Glossary.Commands;

namespace Sddp.Application.Features.Glossary.Validators;

public sealed class CreateGlossaryTermCommandValidator : AbstractValidator<CreateGlossaryTermCommand>
{
    public CreateGlossaryTermCommandValidator()
    {
        RuleFor(x => x.TenantId).Must(NotEmptyId).WithMessage("TenantId is required.");
        RuleFor(x => x.ProjectId).Must(NotEmptyId).WithMessage("ProjectId is required.");
        RuleFor(x => x.UserId).Must(NotEmptyId).WithMessage("UserId is required.");
        RuleFor(x => x.Dto.Term).NotEmpty().WithMessage("Term is required.");
        RuleFor(x => x.Dto.Definition).NotEmpty().WithMessage("Definition is required.");
    }

    private static bool NotEmptyId(GlobalUniqueId id) => !id.IsEmpty;
}

public sealed class UpdateGlossaryTermCommandValidator : AbstractValidator<UpdateGlossaryTermCommand>
{
    public UpdateGlossaryTermCommandValidator()
    {
        RuleFor(x => x.TenantId).Must(NotEmptyId).WithMessage("TenantId is required.");
        RuleFor(x => x.ProjectId).Must(NotEmptyId).WithMessage("ProjectId is required.");
        RuleFor(x => x.TermId).Must(NotEmptyId).WithMessage("TermId is required.");
        RuleFor(x => x.UserId).Must(NotEmptyId).WithMessage("UserId is required.");
    }

    private static bool NotEmptyId(GlobalUniqueId id) => !id.IsEmpty;
}

public sealed class ApproveGlossaryTermCommandValidator : AbstractValidator<ApproveGlossaryTermCommand>
{
    public ApproveGlossaryTermCommandValidator()
    {
        RuleFor(x => x.TenantId).Must(NotEmptyId).WithMessage("TenantId is required.");
        RuleFor(x => x.ProjectId).Must(NotEmptyId).WithMessage("ProjectId is required.");
        RuleFor(x => x.TermId).Must(NotEmptyId).WithMessage("TermId is required.");
        RuleFor(x => x.UserId).Must(NotEmptyId).WithMessage("UserId is required.");
    }

    private static bool NotEmptyId(GlobalUniqueId id) => !id.IsEmpty;
}

public sealed class DeprecateGlossaryTermCommandValidator : AbstractValidator<DeprecateGlossaryTermCommand>
{
    public DeprecateGlossaryTermCommandValidator()
    {
        RuleFor(x => x.TenantId).Must(NotEmptyId).WithMessage("TenantId is required.");
        RuleFor(x => x.ProjectId).Must(NotEmptyId).WithMessage("ProjectId is required.");
        RuleFor(x => x.TermId).Must(NotEmptyId).WithMessage("TermId is required.");
        RuleFor(x => x.UserId).Must(NotEmptyId).WithMessage("UserId is required.");
    }

    private static bool NotEmptyId(GlobalUniqueId id) => !id.IsEmpty;
}

public sealed class ReactivateGlossaryTermCommandValidator : AbstractValidator<ReactivateGlossaryTermCommand>
{
    public ReactivateGlossaryTermCommandValidator()
    {
        RuleFor(x => x.TenantId).Must(NotEmptyId).WithMessage("TenantId is required.");
        RuleFor(x => x.ProjectId).Must(NotEmptyId).WithMessage("ProjectId is required.");
        RuleFor(x => x.TermId).Must(NotEmptyId).WithMessage("TermId is required.");
        RuleFor(x => x.UserId).Must(NotEmptyId).WithMessage("UserId is required.");
    }

    private static bool NotEmptyId(GlobalUniqueId id) => !id.IsEmpty;
}

public sealed class DeleteGlossaryTermCommandValidator : AbstractValidator<DeleteGlossaryTermCommand>
{
    public DeleteGlossaryTermCommandValidator()
    {
        RuleFor(x => x.TenantId).Must(NotEmptyId).WithMessage("TenantId is required.");
        RuleFor(x => x.ProjectId).Must(NotEmptyId).WithMessage("ProjectId is required.");
        RuleFor(x => x.TermId).Must(NotEmptyId).WithMessage("TermId is required.");
    }

    private static bool NotEmptyId(GlobalUniqueId id) => !id.IsEmpty;
}

public sealed class AddGlossaryRelatedTermCommandValidator : AbstractValidator<AddGlossaryRelatedTermCommand>
{
    public AddGlossaryRelatedTermCommandValidator()
    {
        RuleFor(x => x.TenantId).Must(NotEmptyId).WithMessage("TenantId is required.");
        RuleFor(x => x.ProjectId).Must(NotEmptyId).WithMessage("ProjectId is required.");
        RuleFor(x => x.TermId).Must(NotEmptyId).WithMessage("TermId is required.");
        RuleFor(x => x.RelatedTermId).Must(NotEmptyId).WithMessage("RelatedTermId is required.");
    }

    private static bool NotEmptyId(GlobalUniqueId id) => !id.IsEmpty;
}

public sealed class AddGlossaryUsageExampleCommandValidator : AbstractValidator<AddGlossaryUsageExampleCommand>
{
    public AddGlossaryUsageExampleCommandValidator()
    {
        RuleFor(x => x.TenantId).Must(NotEmptyId).WithMessage("TenantId is required.");
        RuleFor(x => x.ProjectId).Must(NotEmptyId).WithMessage("ProjectId is required.");
        RuleFor(x => x.TermId).Must(NotEmptyId).WithMessage("TermId is required.");
        RuleFor(x => x.Example).NotEmpty().WithMessage("Example is required.");
    }

    private static bool NotEmptyId(GlobalUniqueId id) => !id.IsEmpty;
}
