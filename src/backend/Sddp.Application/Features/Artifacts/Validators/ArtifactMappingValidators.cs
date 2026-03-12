using FluentValidation;
using Sddp.Abstractions.ValueObjects;
using Sddp.Application.Features.Artifacts.Commands;

namespace Sddp.Application.Features.Artifacts.Validators;

public sealed class CreateArtifactMappingCommandValidator : AbstractValidator<CreateArtifactMappingCommand>
{
    public CreateArtifactMappingCommandValidator()
    {
        RuleFor(x => x.TenantId).Must(NotEmptyId).WithMessage("TenantId is required.");
        RuleFor(x => x.ProjectId).Must(NotEmptyId).WithMessage("ProjectId is required.");
        RuleFor(x => x.SpecId).Must(NotEmptyId).WithMessage("SpecId is required.");
        RuleFor(x => x.ArtifactPath).NotEmpty().WithMessage("ArtifactPath is required.");
        RuleFor(x => x.UserId).Must(NotEmptyId).WithMessage("UserId is required.");
    }

    private static bool NotEmptyId(GlobalUniqueId id) => !id.IsEmpty;
}

public sealed class UpdateMappingSourceCommandValidator : AbstractValidator<UpdateMappingSourceCommand>
{
    public UpdateMappingSourceCommandValidator()
    {
        RuleFor(x => x.TenantId).Must(NotEmptyId).WithMessage("TenantId is required.");
        RuleFor(x => x.ProjectId).Must(NotEmptyId).WithMessage("ProjectId is required.");
        RuleFor(x => x.MappingId).Must(NotEmptyId).WithMessage("MappingId is required.");
        RuleFor(x => x.UserId).Must(NotEmptyId).WithMessage("UserId is required.");
    }

    private static bool NotEmptyId(GlobalUniqueId id) => !id.IsEmpty;
}

public sealed class DeleteArtifactMappingCommandValidator : AbstractValidator<DeleteArtifactMappingCommand>
{
    public DeleteArtifactMappingCommandValidator()
    {
        RuleFor(x => x.TenantId).Must(NotEmptyId).WithMessage("TenantId is required.");
        RuleFor(x => x.ProjectId).Must(NotEmptyId).WithMessage("ProjectId is required.");
        RuleFor(x => x.MappingId).Must(NotEmptyId).WithMessage("MappingId is required.");
        RuleFor(x => x.UserId).Must(NotEmptyId).WithMessage("UserId is required.");
    }

    private static bool NotEmptyId(GlobalUniqueId id) => !id.IsEmpty;
}
