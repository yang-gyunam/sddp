using FluentValidation;
using Sddp.Abstractions.ValueObjects;
using Sddp.Application.Features.Relationships.Commands;

namespace Sddp.Application.Features.Relationships.Validators;

public sealed class CreateRelationshipCommandValidator : AbstractValidator<CreateRelationshipCommand>
{
    public CreateRelationshipCommandValidator()
    {
        RuleFor(x => x.TenantId).Must(NotEmptyId).WithMessage("TenantId is required.");
        RuleFor(x => x.ProjectId).Must(NotEmptyId).WithMessage("ProjectId is required.");
        RuleFor(x => x.UserId).Must(NotEmptyId).WithMessage("UserId is required.");
        RuleFor(x => x.Dto.FromEntityType).NotEmpty().WithMessage("FromEntityType is required.");
        RuleFor(x => x.Dto.FromEntityId).NotEmpty().WithMessage("FromEntityId is required.");
        RuleFor(x => x.Dto.ToEntityType).NotEmpty().WithMessage("ToEntityType is required.");
        RuleFor(x => x.Dto.ToEntityId).NotEmpty().WithMessage("ToEntityId is required.");
        RuleFor(x => x.Dto.Type).NotEmpty().WithMessage("Type is required.");
    }

    private static bool NotEmptyId(GlobalUniqueId id) => !id.IsEmpty;
}

public sealed class InvalidateRelationshipCommandValidator : AbstractValidator<InvalidateRelationshipCommand>
{
    public InvalidateRelationshipCommandValidator()
    {
        RuleFor(x => x.TenantId).Must(NotEmptyId).WithMessage("TenantId is required.");
        RuleFor(x => x.ProjectId).Must(NotEmptyId).WithMessage("ProjectId is required.");
        RuleFor(x => x.RelationshipId).Must(NotEmptyId).WithMessage("RelationshipId is required.");
    }

    private static bool NotEmptyId(GlobalUniqueId id) => !id.IsEmpty;
}
