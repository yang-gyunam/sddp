using FluentValidation;
using Sddp.Abstractions.ValueObjects;
using Sddp.Application.Features.Artifacts.Commands;

namespace Sddp.Application.Features.Artifacts.Validators;

public sealed class UpsertArtifactCommandValidator : AbstractValidator<UpsertArtifactCommand>
{
    public UpsertArtifactCommandValidator()
    {
        RuleFor(x => x.TenantId).Must(NotEmptyId).WithMessage("TenantId is required.");
        RuleFor(x => x.ProjectId).Must(NotEmptyId).WithMessage("ProjectId is required.");
        RuleFor(x => x.SpecId).Must(NotEmptyId).WithMessage("SpecId is required.");
        RuleFor(x => x.ArtifactPath).NotEmpty().WithMessage("ArtifactPath is required.");
        RuleFor(x => x.ContentHash).NotEmpty().WithMessage("ContentHash is required.");
    }

    private static bool NotEmptyId(GlobalUniqueId id) => !id.IsEmpty;
}

public sealed class RegenerateArtifactCommandValidator : AbstractValidator<RegenerateArtifactCommand>
{
    public RegenerateArtifactCommandValidator()
    {
        RuleFor(x => x.TenantId).Must(NotEmptyId).WithMessage("TenantId is required.");
        RuleFor(x => x.ProjectId).Must(NotEmptyId).WithMessage("ProjectId is required.");
        RuleFor(x => x.ArtifactId).Must(NotEmptyId).WithMessage("ArtifactId is required.");
    }

    private static bool NotEmptyId(GlobalUniqueId id) => !id.IsEmpty;
}

public sealed class VerifyArtifactCommandValidator : AbstractValidator<VerifyArtifactCommand>
{
    public VerifyArtifactCommandValidator()
    {
        RuleFor(x => x.TenantId).Must(NotEmptyId).WithMessage("TenantId is required.");
        RuleFor(x => x.ProjectId).Must(NotEmptyId).WithMessage("ProjectId is required.");
        RuleFor(x => x.ArtifactId).Must(NotEmptyId).WithMessage("ArtifactId is required.");
        RuleFor(x => x.CurrentHash).NotEmpty().WithMessage("CurrentHash is required.");
    }

    private static bool NotEmptyId(GlobalUniqueId id) => !id.IsEmpty;
}
