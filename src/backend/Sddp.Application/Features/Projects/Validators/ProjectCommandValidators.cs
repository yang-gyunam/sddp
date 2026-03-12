using FluentValidation;
using Sddp.Abstractions.Enums;
using Sddp.Application.Features.Projects.Commands;

namespace Sddp.Application.Features.Projects.Validators;

public sealed class CreateProjectCommandValidator : AbstractValidator<CreateProjectCommand>
{
    public CreateProjectCommandValidator()
    {
        RuleFor(x => x.TenantId).Must(id => !id.IsEmpty).WithMessage("TenantId is required.");
        RuleFor(x => x.RequestUserId).Must(id => !id.IsEmpty).WithMessage("RequestUserId is required.");
        RuleFor(x => x.Dto.Code)
            .NotEmpty().WithMessage("Project code is required.")
            .Length(2, 30).WithMessage("Project code must be 2-30 characters.")
            .Matches(@"^[A-Z][A-Z0-9_]*$").WithMessage("Project code must start with uppercase letter and contain only uppercase letters, digits, and underscores.");
        RuleFor(x => x.Dto.Name)
            .NotEmpty().WithMessage("Project name is required.")
            .Length(2, 100).WithMessage("Project name must be 2-100 characters.");
        RuleFor(x => x.Dto.Description)
            .MaximumLength(500).WithMessage("Description must not exceed 500 characters.")
            .When(x => x.Dto.Description is not null);
    }
}

public sealed class UpdateProjectCommandValidator : AbstractValidator<UpdateProjectCommand>
{
    public UpdateProjectCommandValidator()
    {
        RuleFor(x => x.TenantId).Must(id => !id.IsEmpty).WithMessage("TenantId is required.");
        RuleFor(x => x.ProjectId).Must(id => !id.IsEmpty).WithMessage("ProjectId is required.");
        RuleFor(x => x.RequestUserId).Must(id => !id.IsEmpty).WithMessage("RequestUserId is required.");
        RuleFor(x => x.Dto.Name)
            .NotEmpty().WithMessage("Project name is required.")
            .MaximumLength(200).WithMessage("Project name must not exceed 200 characters.");
        RuleFor(x => x.Dto.Description)
            .MaximumLength(2000).WithMessage("Description must not exceed 2000 characters.")
            .When(x => x.Dto.Description is not null);
    }
}

public sealed class AddProjectMemberCommandValidator : AbstractValidator<AddProjectMemberCommand>
{
    public AddProjectMemberCommandValidator()
    {
        RuleFor(x => x.TenantId).Must(id => !id.IsEmpty).WithMessage("TenantId is required.");
        RuleFor(x => x.ProjectId).Must(id => !id.IsEmpty).WithMessage("ProjectId is required.");
        RuleFor(x => x.TargetUserId).Must(id => !id.IsEmpty).WithMessage("UserId is required.");
        RuleFor(x => x.RequestUserId).Must(id => !id.IsEmpty).WithMessage("RequestUserId is required.");
        RuleFor(x => x.RoleName)
            .NotEmpty().WithMessage("Role is required.")
            .Must(role => Enum.TryParse<RoleType>(role, out _))
            .WithMessage("Invalid role name. Valid roles: ProductOwner, DomainExpert, Developer, Reviewer, QATester.");
    }
}
