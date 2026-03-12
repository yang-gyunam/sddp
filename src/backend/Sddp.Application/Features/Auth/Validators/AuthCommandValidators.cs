using FluentValidation;
using Sddp.Application.Features.Auth.Commands;

namespace Sddp.Application.Features.Auth.Validators;

public sealed class LoginCommandValidator : AbstractValidator<LoginCommand>
{
    public LoginCommandValidator()
    {
        RuleFor(x => x.Request).NotNull().WithMessage("Request is required.");
        RuleFor(x => x.Request.Username).NotEmpty().WithMessage("Username is required.")
            .When(x => x.Request != null);
        RuleFor(x => x.Request.Password).NotEmpty().WithMessage("Password is required.")
            .When(x => x.Request != null);
    }
}

public sealed class RefreshTokenCommandValidator : AbstractValidator<RefreshTokenCommand>
{
    public RefreshTokenCommandValidator()
    {
        RuleFor(x => x.RefreshToken).NotEmpty().WithMessage("RefreshToken is required.");
    }
}

public sealed class LogoutCommandValidator : AbstractValidator<LogoutCommand>
{
    public LogoutCommandValidator()
    {
        // RefreshToken is nullable - logout can be called without it
        // No validation rules needed
    }
}

public sealed class ChangePasswordCommandValidator : AbstractValidator<ChangePasswordCommand>
{
    public ChangePasswordCommandValidator()
    {
        RuleFor(x => x.UserId).Must(id => !id.IsEmpty).WithMessage("UserId is required.");
        RuleFor(x => x.Request).NotNull().WithMessage("Request is required.");
        RuleFor(x => x.Request.CurrentPassword).NotEmpty().WithMessage("Current password is required.")
            .When(x => x.Request != null);
        RuleFor(x => x.Request.NewPassword)
            .NotEmpty().WithMessage("New password is required.")
            .MinimumLength(8).WithMessage("Password must be at least 8 characters.")
            .Matches("[A-Z]").WithMessage("Password must contain at least one uppercase letter.")
            .Matches("[a-z]").WithMessage("Password must contain at least one lowercase letter.")
            .Matches("[0-9]").WithMessage("Password must contain at least one digit.")
            .Matches(@"[^a-zA-Z0-9]").WithMessage("Password must contain at least one special character.")
            .When(x => x.Request != null);
    }
}
