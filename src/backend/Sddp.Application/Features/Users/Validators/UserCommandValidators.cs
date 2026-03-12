using FluentValidation;
using Sddp.Abstractions.Enums;
using Sddp.Abstractions.ValueObjects;
using Sddp.Application.Features.Users.Commands;

namespace Sddp.Application.Features.Users.Validators;

public sealed class UpdateCurrentUserProfileCommandValidator : AbstractValidator<UpdateCurrentUserProfileCommand>
{
    public UpdateCurrentUserProfileCommandValidator()
    {
        RuleFor(x => x.UserId).Must(NotEmptyId).WithMessage("UserId is required.");
        RuleFor(x => x.Dto.DisplayName).NotEmpty().WithMessage("Display name is required.");
        RuleFor(x => x.Dto.Email).NotEmpty().WithMessage("Email is required.");
    }

    private static bool NotEmptyId(GlobalUniqueId id) => !id.IsEmpty;
}

public sealed class CreateUserCommandValidator : AbstractValidator<CreateUserCommand>
{
    public CreateUserCommandValidator()
    {
        RuleFor(x => x.TenantId).Must(id => !id.IsEmpty).WithMessage("TenantId is required.");
        RuleFor(x => x.RequestUserId).Must(id => !id.IsEmpty).WithMessage("RequestUserId is required.");
        RuleFor(x => x.Dto.Username).NotEmpty().Length(3, 50).WithMessage("Username must be 3-50 characters.");
        RuleFor(x => x.Dto.Email).NotEmpty().EmailAddress().WithMessage("Valid email is required.");
        RuleFor(x => x.Dto.DisplayName).NotEmpty().Length(2, 100).WithMessage("Display name must be 2-100 characters.");
        RuleFor(x => x.Dto.Password).NotEmpty().MinimumLength(6).WithMessage("Password must be at least 6 characters.");
    }
}

public sealed class AdminResetPasswordCommandValidator : AbstractValidator<AdminResetPasswordCommand>
{
    public AdminResetPasswordCommandValidator()
    {
        RuleFor(x => x.UserId).Must(id => !id.IsEmpty).WithMessage("UserId is required.");
        RuleFor(x => x.RequestUserId).Must(id => !id.IsEmpty).WithMessage("RequestUserId is required.");
    }
}

public sealed class UpdateSystemUserCommandValidator : AbstractValidator<UpdateSystemUserCommand>
{
    private static readonly string[] ValidGlobalRoles = Enum.GetNames<RoleType>();

    public UpdateSystemUserCommandValidator()
    {
        RuleFor(x => x.TenantId).Must(id => !id.IsEmpty).WithMessage("TenantId is required.");
        RuleFor(x => x.UserId).Must(id => !id.IsEmpty).WithMessage("UserId is required.");
        RuleFor(x => x.RequestUserId).Must(id => !id.IsEmpty).WithMessage("RequestUserId is required.");
        RuleFor(x => x.Dto.DisplayName).NotEmpty().MaximumLength(200).WithMessage("Display name is required (max 200 chars).");
        RuleFor(x => x.Dto.Email).NotEmpty().EmailAddress().MaximumLength(300).WithMessage("Valid email is required.");
        RuleFor(x => x.Dto.GlobalRole)
            .Must(r => r is null || ValidGlobalRoles.Contains(r, StringComparer.OrdinalIgnoreCase))
            .WithMessage($"GlobalRole must be one of: {string.Join(", ", ValidGlobalRoles)}, or null.");
    }
}
