using FluentValidation;
using Sddp.Application.Features.SystemConfig.Commands;

namespace Sddp.Application.Features.SystemConfig.Validators;

public sealed class SetSystemConfigValueCommandValidator : AbstractValidator<SetSystemConfigValueCommand>
{
    public SetSystemConfigValueCommandValidator()
    {
        RuleFor(x => x.ConfigGroup).NotEmpty().WithMessage("ConfigGroup is required.");
        RuleFor(x => x.ConfigKey).NotEmpty().WithMessage("ConfigKey is required.");
    }
}

public sealed class SetSystemConfigGroupCommandValidator : AbstractValidator<SetSystemConfigGroupCommand>
{
    public SetSystemConfigGroupCommandValidator()
    {
        RuleFor(x => x.ConfigGroup).NotEmpty().WithMessage("ConfigGroup is required.");
        RuleFor(x => x.Values).NotNull().WithMessage("Values are required.");
    }
}

public sealed class SaveSystemConfigCommandValidator : AbstractValidator<SaveSystemConfigCommand>
{
    public SaveSystemConfigCommandValidator()
    {
        RuleFor(x => x.Dto).NotNull().WithMessage("Dto is required.");
    }
}

public sealed class DeleteSystemConfigValueCommandValidator : AbstractValidator<DeleteSystemConfigValueCommand>
{
    public DeleteSystemConfigValueCommandValidator()
    {
        RuleFor(x => x.ConfigGroup).NotEmpty().WithMessage("ConfigGroup is required.");
        RuleFor(x => x.ConfigKey).NotEmpty().WithMessage("ConfigKey is required.");
    }
}

public sealed class ResetSystemConfigCommandValidator : AbstractValidator<ResetSystemConfigCommand>
{
    public ResetSystemConfigCommandValidator()
    {
        // Both TenantId and ProjectId are nullable - reset can target system-level config
        // No validation rules needed beyond type safety
    }
}
