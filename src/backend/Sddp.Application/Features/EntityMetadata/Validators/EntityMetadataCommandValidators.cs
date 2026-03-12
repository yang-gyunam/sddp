using FluentValidation;
using Sddp.Abstractions.ValueObjects;
using Sddp.Application.Features.EntityMetadata.Commands;

namespace Sddp.Application.Features.EntityMetadata.Validators;

public sealed class CreateEntityMetadataCommandValidator : AbstractValidator<CreateEntityMetadataCommand>
{
    public CreateEntityMetadataCommandValidator()
    {
        RuleFor(x => x.SpecId).NotEmpty().WithMessage("SpecId is required.");
        RuleFor(x => x.TenantId).Must(NotEmptyId).WithMessage("TenantId is required.");
        RuleFor(x => x.ProjectId).Must(NotEmptyId).WithMessage("ProjectId is required.");
        RuleFor(x => x.Dto).NotNull().WithMessage("Dto is required.");
        RuleFor(x => x.Dto.EntityName).NotEmpty().WithMessage("EntityName is required.")
            .When(x => x.Dto != null);
        RuleFor(x => x.Dto.TableName).NotEmpty().WithMessage("TableName is required.")
            .When(x => x.Dto != null);
    }

    private static bool NotEmptyId(GlobalUniqueId id) => !id.IsEmpty;
}

public sealed class UpdateEntityMetadataCommandValidator : AbstractValidator<UpdateEntityMetadataCommand>
{
    public UpdateEntityMetadataCommandValidator()
    {
        RuleFor(x => x.SpecId).NotEmpty().WithMessage("SpecId is required.");
        RuleFor(x => x.EntityId).NotEmpty().WithMessage("EntityId is required.");
        RuleFor(x => x.TenantId).Must(NotEmptyId).WithMessage("TenantId is required.");
        RuleFor(x => x.ProjectId).Must(NotEmptyId).WithMessage("ProjectId is required.");
        RuleFor(x => x.Dto).NotNull().WithMessage("Dto is required.");
        RuleFor(x => x.Dto.EntityName).NotEmpty().WithMessage("EntityName is required.")
            .When(x => x.Dto != null);
        RuleFor(x => x.Dto.TableName).NotEmpty().WithMessage("TableName is required.")
            .When(x => x.Dto != null);
    }

    private static bool NotEmptyId(GlobalUniqueId id) => !id.IsEmpty;
}

public sealed class DeleteEntityMetadataCommandValidator : AbstractValidator<DeleteEntityMetadataCommand>
{
    public DeleteEntityMetadataCommandValidator()
    {
        RuleFor(x => x.SpecId).NotEmpty().WithMessage("SpecId is required.");
        RuleFor(x => x.EntityId).NotEmpty().WithMessage("EntityId is required.");
        RuleFor(x => x.TenantId).Must(NotEmptyId).WithMessage("TenantId is required.");
        RuleFor(x => x.ProjectId).Must(NotEmptyId).WithMessage("ProjectId is required.");
    }

    private static bool NotEmptyId(GlobalUniqueId id) => !id.IsEmpty;
}
