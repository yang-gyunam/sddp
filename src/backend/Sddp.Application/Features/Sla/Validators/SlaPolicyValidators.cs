using FluentValidation;
using Sddp.Abstractions.ValueObjects;
using Sddp.Application.Features.Sla.Commands;

namespace Sddp.Application.Features.Sla.Validators;

public sealed class CreateSlaPolicyCommandValidator : AbstractValidator<CreateSlaPolicyCommand>
{
    private static readonly string[] ValidSlaTypes = ["signoff", "review", "decision"];

    public CreateSlaPolicyCommandValidator()
    {
        RuleFor(x => x.TenantId).Must(NotEmptyId).WithMessage("TenantId is required.");
        RuleFor(x => x.ProjectId).Must(NotEmptyId).WithMessage("ProjectId is required.");
        RuleFor(x => x.SlaType).NotEmpty().Must(t => ValidSlaTypes.Contains(t.ToLowerInvariant()))
            .WithMessage("SlaType must be one of: signoff, review, decision.");
        RuleFor(x => x.SlaHours).GreaterThan(0).WithMessage("SlaHours must be greater than 0.");
        RuleFor(x => x.UrgentSlaHours).GreaterThan(0).WithMessage("UrgentSlaHours must be greater than 0.");
        RuleFor(x => x.UrgentSlaHours).LessThan(x => x.SlaHours)
            .WithMessage("UrgentSlaHours must be less than SlaHours.");
    }

    private static bool NotEmptyId(GlobalUniqueId id) => !id.IsEmpty;
}

public sealed class UpdateSlaPolicyCommandValidator : AbstractValidator<UpdateSlaPolicyCommand>
{
    public UpdateSlaPolicyCommandValidator()
    {
        RuleFor(x => x.TenantId).Must(NotEmptyId).WithMessage("TenantId is required.");
        RuleFor(x => x.ProjectId).Must(NotEmptyId).WithMessage("ProjectId is required.");
        RuleFor(x => x.PolicyId).Must(NotEmptyId).WithMessage("PolicyId is required.");
        RuleFor(x => x.SlaHours).GreaterThan(0).WithMessage("SlaHours must be greater than 0.");
        RuleFor(x => x.UrgentSlaHours).GreaterThan(0).WithMessage("UrgentSlaHours must be greater than 0.");
        RuleFor(x => x.UrgentSlaHours).LessThan(x => x.SlaHours)
            .WithMessage("UrgentSlaHours must be less than SlaHours.");
    }

    private static bool NotEmptyId(GlobalUniqueId id) => !id.IsEmpty;
}

public sealed class DeleteSlaPolicyCommandValidator : AbstractValidator<DeleteSlaPolicyCommand>
{
    public DeleteSlaPolicyCommandValidator()
    {
        RuleFor(x => x.TenantId).Must(NotEmptyId).WithMessage("TenantId is required.");
        RuleFor(x => x.ProjectId).Must(NotEmptyId).WithMessage("ProjectId is required.");
        RuleFor(x => x.PolicyId).Must(NotEmptyId).WithMessage("PolicyId is required.");
    }

    private static bool NotEmptyId(GlobalUniqueId id) => !id.IsEmpty;
}
