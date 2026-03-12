using FluentValidation;
using Sddp.Abstractions.ValueObjects;
using Sddp.Application.Features.Conversations.Topics.Commands;

namespace Sddp.Application.Features.Conversations.Topics.Validators;

public sealed class CreateTopicCommandValidator : AbstractValidator<CreateTopicCommand>
{
    public CreateTopicCommandValidator()
    {
        RuleFor(x => x.TenantId).Must(NotEmptyId).WithMessage("TenantId is required.");
        RuleFor(x => x.ForumId).Must(NotEmptyId).WithMessage("ForumId is required.");
        RuleFor(x => x.AuthorId).Must(NotEmptyId).WithMessage("AuthorId is required.");
        RuleFor(x => x.Title).NotEmpty().WithMessage("Title is required.");
    }

    private static bool NotEmptyId(GlobalUniqueId id) => !id.IsEmpty;
}

public sealed class UpdateTopicCommandValidator : AbstractValidator<UpdateTopicCommand>
{
    public UpdateTopicCommandValidator()
    {
        RuleFor(x => x.TenantId).Must(NotEmptyId).WithMessage("TenantId is required.");
        RuleFor(x => x.TopicId).Must(NotEmptyId).WithMessage("TopicId is required.");
        RuleFor(x => x.Title).NotEmpty().WithMessage("Title is required.");
    }

    private static bool NotEmptyId(GlobalUniqueId id) => !id.IsEmpty;
}

public sealed class CloseTopicCommandValidator : AbstractValidator<CloseTopicCommand>
{
    public CloseTopicCommandValidator()
    {
        RuleFor(x => x.TenantId).Must(NotEmptyId).WithMessage("TenantId is required.");
        RuleFor(x => x.TopicId).Must(NotEmptyId).WithMessage("TopicId is required.");
        RuleFor(x => x.DecisionSpecId)
            .Must(BeValidOptionalId)
            .WithMessage("DecisionSpecId must be a valid ID.");
    }

    private static bool NotEmptyId(GlobalUniqueId id) => !id.IsEmpty;
    private static bool BeValidOptionalId(GlobalUniqueId? id) => id is null || !id.Value.IsEmpty;
}

public sealed class ReopenTopicCommandValidator : AbstractValidator<ReopenTopicCommand>
{
    public ReopenTopicCommandValidator()
    {
        RuleFor(x => x.TenantId).Must(NotEmptyId).WithMessage("TenantId is required.");
        RuleFor(x => x.TopicId).Must(NotEmptyId).WithMessage("TopicId is required.");
    }

    private static bool NotEmptyId(GlobalUniqueId id) => !id.IsEmpty;
}

public sealed class ArchiveTopicCommandValidator : AbstractValidator<ArchiveTopicCommand>
{
    public ArchiveTopicCommandValidator()
    {
        RuleFor(x => x.TenantId).Must(NotEmptyId).WithMessage("TenantId is required.");
        RuleFor(x => x.TopicId).Must(NotEmptyId).WithMessage("TopicId is required.");
    }

    private static bool NotEmptyId(GlobalUniqueId id) => !id.IsEmpty;
}

public sealed class ToggleTopicPinCommandValidator : AbstractValidator<ToggleTopicPinCommand>
{
    public ToggleTopicPinCommandValidator()
    {
        RuleFor(x => x.TenantId).Must(NotEmptyId).WithMessage("TenantId is required.");
        RuleFor(x => x.TopicId).Must(NotEmptyId).WithMessage("TopicId is required.");
    }

    private static bool NotEmptyId(GlobalUniqueId id) => !id.IsEmpty;
}

public sealed class ToggleTopicLockCommandValidator : AbstractValidator<ToggleTopicLockCommand>
{
    public ToggleTopicLockCommandValidator()
    {
        RuleFor(x => x.TenantId).Must(NotEmptyId).WithMessage("TenantId is required.");
        RuleFor(x => x.TopicId).Must(NotEmptyId).WithMessage("TopicId is required.");
    }

    private static bool NotEmptyId(GlobalUniqueId id) => !id.IsEmpty;
}

public sealed class PostTopicMessageCommandValidator : AbstractValidator<PostTopicMessageCommand>
{
    public PostTopicMessageCommandValidator()
    {
        RuleFor(x => x.TenantId).Must(NotEmptyId).WithMessage("TenantId is required.");
        RuleFor(x => x.TopicId).Must(NotEmptyId).WithMessage("TopicId is required.");
        RuleFor(x => x.SenderUserId).Must(NotEmptyId).WithMessage("SenderUserId is required.");
        RuleFor(x => x.ReplyToId)
            .Must(BeValidOptionalId)
            .WithMessage("ReplyToId must be a valid ID.");
    }

    private static bool NotEmptyId(GlobalUniqueId id) => !id.IsEmpty;
    private static bool BeValidOptionalId(GlobalUniqueId? id) => id is null || !id.Value.IsEmpty;
}
