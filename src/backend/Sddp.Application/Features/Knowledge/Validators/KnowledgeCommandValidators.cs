using FluentValidation;
using Sddp.Abstractions.ValueObjects;
using Sddp.Application.Features.Knowledge.Commands;

namespace Sddp.Application.Features.Knowledge.Validators;

public sealed class CreateKnowledgePageCommandValidator
    : AbstractValidator<CreateKnowledgePageCommand>
{
    public CreateKnowledgePageCommandValidator()
    {
        RuleFor(command => command.TenantId).Must(NotEmptyId);
        RuleFor(command => command.ProjectId).Must(NotEmptyId);
        RuleFor(command => command.UserId).Must(NotEmptyId);
        RuleFor(command => command.Dto.Title).NotEmpty().MaximumLength(200);
        RuleFor(command => command.Dto.ParentId)
            .Must(BeValidIdOrEmpty)
            .WithMessage("ParentId must be a valid ID.");
        RuleFor(command => command.Dto.Template).MaximumLength(100);
    }

    private static bool NotEmptyId(GlobalUniqueId id) => !id.IsEmpty;
    private static bool BeValidIdOrEmpty(string? value) =>
        string.IsNullOrWhiteSpace(value) || GlobalUniqueId.TryParse(value, out _);
}

public sealed class UpdateKnowledgePageCommandValidator
    : AbstractValidator<UpdateKnowledgePageCommand>
{
    public UpdateKnowledgePageCommandValidator()
    {
        RuleFor(command => command.TenantId).Must(NotEmptyId);
        RuleFor(command => command.ProjectId).Must(NotEmptyId);
        RuleFor(command => command.PageId).Must(NotEmptyId);
        RuleFor(command => command.UserId).Must(NotEmptyId);
        RuleFor(command => command.ExpectedRevision).GreaterThan(0);
        RuleFor(command => command.Dto.Title).NotEmpty().MaximumLength(200);
        RuleFor(command => command.Dto.Slug).MaximumLength(200);
        RuleFor(command => command.Dto.ChangeSummary).MaximumLength(500);
        RuleFor(command => command.Dto.Content)
            .Must(content => content.ValueKind == System.Text.Json.JsonValueKind.Object)
            .WithMessage("Content must be a TipTap document object.");
    }

    private static bool NotEmptyId(GlobalUniqueId id) => !id.IsEmpty;
}

public sealed class MoveKnowledgePageCommandValidator
    : AbstractValidator<MoveKnowledgePageCommand>
{
    public MoveKnowledgePageCommandValidator()
    {
        RuleFor(command => command.TenantId).Must(NotEmptyId);
        RuleFor(command => command.ProjectId).Must(NotEmptyId);
        RuleFor(command => command.PageId).Must(NotEmptyId);
        RuleFor(command => command.UserId).Must(NotEmptyId);
        RuleFor(command => command.ExpectedRevision).GreaterThan(0);
        RuleFor(command => command.Dto.ParentId)
            .Must(BeValidIdOrEmpty)
            .WithMessage("ParentId must be a valid ID.");
        RuleFor(command => command.Dto.Position).GreaterThanOrEqualTo(0);
    }

    private static bool NotEmptyId(GlobalUniqueId id) => !id.IsEmpty;
    private static bool BeValidIdOrEmpty(string? value) =>
        string.IsNullOrWhiteSpace(value) || GlobalUniqueId.TryParse(value, out _);
}

public sealed class PublishKnowledgePageCommandValidator
    : AbstractValidator<PublishKnowledgePageCommand>
{
    public PublishKnowledgePageCommandValidator()
    {
        AddMutationRules(this);
    }

    private static void AddMutationRules(AbstractValidator<PublishKnowledgePageCommand> validator)
    {
        validator.RuleFor(command => command.TenantId).Must(id => !id.IsEmpty);
        validator.RuleFor(command => command.ProjectId).Must(id => !id.IsEmpty);
        validator.RuleFor(command => command.PageId).Must(id => !id.IsEmpty);
        validator.RuleFor(command => command.UserId).Must(id => !id.IsEmpty);
        validator.RuleFor(command => command.ExpectedRevision).GreaterThan(0);
    }
}

public sealed class ArchiveKnowledgePageCommandValidator
    : AbstractValidator<ArchiveKnowledgePageCommand>
{
    public ArchiveKnowledgePageCommandValidator()
    {
        RuleFor(command => command.TenantId).Must(id => !id.IsEmpty);
        RuleFor(command => command.ProjectId).Must(id => !id.IsEmpty);
        RuleFor(command => command.PageId).Must(id => !id.IsEmpty);
        RuleFor(command => command.UserId).Must(id => !id.IsEmpty);
        RuleFor(command => command.ExpectedRevision).GreaterThan(0);
    }
}

public sealed class RestoreKnowledgePageCommandValidator
    : AbstractValidator<RestoreKnowledgePageCommand>
{
    public RestoreKnowledgePageCommandValidator()
    {
        RuleFor(command => command.TenantId).Must(id => !id.IsEmpty);
        RuleFor(command => command.ProjectId).Must(id => !id.IsEmpty);
        RuleFor(command => command.PageId).Must(id => !id.IsEmpty);
        RuleFor(command => command.UserId).Must(id => !id.IsEmpty);
        RuleFor(command => command.ExpectedRevision).GreaterThan(0);
    }
}

public sealed class RestoreKnowledgePageVersionCommandValidator
    : AbstractValidator<RestoreKnowledgePageVersionCommand>
{
    public RestoreKnowledgePageVersionCommandValidator()
    {
        RuleFor(command => command.TenantId).Must(id => !id.IsEmpty);
        RuleFor(command => command.ProjectId).Must(id => !id.IsEmpty);
        RuleFor(command => command.PageId).Must(id => !id.IsEmpty);
        RuleFor(command => command.UserId).Must(id => !id.IsEmpty);
        RuleFor(command => command.Revision).GreaterThan(0);
        RuleFor(command => command.ExpectedRevision).GreaterThan(0);
    }
}
