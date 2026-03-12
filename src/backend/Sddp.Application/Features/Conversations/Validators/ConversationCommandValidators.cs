using FluentValidation;
using Sddp.Abstractions.ValueObjects;
using Sddp.Application.Features.Conversations.Commands;

namespace Sddp.Application.Features.Conversations.Validators;

public sealed class CreateConversationCommandValidator : AbstractValidator<CreateConversationCommand>
{
    public CreateConversationCommandValidator()
    {
        RuleFor(x => x.TenantId).Must(NotEmptyId).WithMessage("TenantId is required.");
        RuleFor(x => x.UserId).Must(NotEmptyId).WithMessage("UserId is required.");
        RuleFor(x => x.Name).NotEmpty().WithMessage("Name is required.");
        RuleFor(x => x.ProjectId)
            .Must(BeValidOptionalId)
            .WithMessage("ProjectId must be a valid ID.");
    }

    private static bool NotEmptyId(GlobalUniqueId id) => !id.IsEmpty;
    private static bool BeValidOptionalId(GlobalUniqueId? id) => id is null || !id.Value.IsEmpty;
}

public sealed class GetConversationMessagesCommandValidator : AbstractValidator<GetConversationMessagesCommand>
{
    public GetConversationMessagesCommandValidator()
    {
        RuleFor(x => x.TenantId).Must(NotEmptyId).WithMessage("TenantId is required.");
        RuleFor(x => x.ProjectId)
            .Must(BeValidOptionalId)
            .WithMessage("ProjectId must be a valid ID.");
        RuleFor(x => x.ConversationId).Must(NotEmptyId).WithMessage("ConversationId is required.");
        RuleFor(x => x.UserId).Must(NotEmptyId).WithMessage("UserId is required.");
    }

    private static bool NotEmptyId(GlobalUniqueId id) => !id.IsEmpty;
    private static bool BeValidOptionalId(GlobalUniqueId? id) => id is null || !id.Value.IsEmpty;
}

public sealed class PostConversationMessageCommandValidator : AbstractValidator<PostConversationMessageCommand>
{
    public PostConversationMessageCommandValidator()
    {
        RuleFor(x => x.TenantId).Must(NotEmptyId).WithMessage("TenantId is required.");
        RuleFor(x => x.ProjectId)
            .Must(BeValidOptionalId)
            .WithMessage("ProjectId must be a valid ID.");
        RuleFor(x => x.ConversationId).Must(NotEmptyId).WithMessage("ConversationId is required.");
        RuleFor(x => x.SenderUserId).Must(NotEmptyId).WithMessage("SenderUserId is required.");
        RuleFor(x => x.ReplyToId)
            .Must(BeValidOptionalId)
            .WithMessage("ReplyToId must be a valid ID.");
    }

    private static bool NotEmptyId(GlobalUniqueId id) => !id.IsEmpty;
    private static bool BeValidOptionalId(GlobalUniqueId? id) => id is null || !id.Value.IsEmpty;
}
