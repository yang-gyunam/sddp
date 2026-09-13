using FluentValidation;
using Sddp.Abstractions.ValueObjects;
using Sddp.Application.Features.EntityMetadata.Commands;
using Sddp.Abstractions.DTOs;

namespace Sddp.Application.Features.EntityMetadata.Validators;

public sealed class CreateEntityMetadataCommandValidator : AbstractValidator<CreateEntityMetadataCommand>
{
    public CreateEntityMetadataCommandValidator()
    {
        RuleFor(x => x.SpecId).NotEmpty().WithMessage("SpecId is required.");
        RuleFor(x => x.TenantId).Must(NotEmptyId).WithMessage("TenantId is required.");
        RuleFor(x => x.ProjectId).Must(NotEmptyId).WithMessage("ProjectId is required.");
        RuleFor(x => x.Dto).NotNull().WithMessage("Dto is required.");
        RuleFor(x => x.Dto.EntityName).NotEmpty().WithMessage("EntityName is required.")
            .When(x => x.Dto != null);
        RuleFor(x => x.Dto.TableName).NotEmpty().WithMessage("TableName is required.")
            .When(x => x.Dto != null);

        RuleFor(x => x.Dto.TableName)
            .Must(MetadataIdentifierRules.IsUsableSqlName)
            .WithMessage(MetadataIdentifierRules.TableNameMessage)
            .When(x => x.Dto != null && !string.IsNullOrWhiteSpace(x.Dto.TableName));

        RuleForEach(x => x.Dto.Fields)
            .SetValidator(new EntityFieldInputDtoValidator())
            .When(x => x.Dto?.Fields != null);
    }

    private static bool NotEmptyId(GlobalUniqueId id) => !id.IsEmpty;
}

public sealed class UpdateEntityMetadataCommandValidator : AbstractValidator<UpdateEntityMetadataCommand>
{
    public UpdateEntityMetadataCommandValidator()
    {
        RuleFor(x => x.SpecId).NotEmpty().WithMessage("SpecId is required.");
        RuleFor(x => x.EntityId).NotEmpty().WithMessage("EntityId is required.");
        RuleFor(x => x.TenantId).Must(NotEmptyId).WithMessage("TenantId is required.");
        RuleFor(x => x.ProjectId).Must(NotEmptyId).WithMessage("ProjectId is required.");
        RuleFor(x => x.Dto).NotNull().WithMessage("Dto is required.");
        RuleFor(x => x.Dto.EntityName).NotEmpty().WithMessage("EntityName is required.")
            .When(x => x.Dto != null);
        RuleFor(x => x.Dto.TableName).NotEmpty().WithMessage("TableName is required.")
            .When(x => x.Dto != null);

        RuleFor(x => x.Dto.TableName)
            .Must(MetadataIdentifierRules.IsUsableSqlName)
            .WithMessage(MetadataIdentifierRules.TableNameMessage)
            .When(x => x.Dto != null && !string.IsNullOrWhiteSpace(x.Dto.TableName));

        RuleForEach(x => x.Dto.Fields)
            .SetValidator(new EntityFieldInputDtoValidator())
            .When(x => x.Dto?.Fields != null);
    }

    private static bool NotEmptyId(GlobalUniqueId id) => !id.IsEmpty;
}

public sealed class DeleteEntityMetadataCommandValidator : AbstractValidator<DeleteEntityMetadataCommand>
{
    public DeleteEntityMetadataCommandValidator()
    {
        RuleFor(x => x.SpecId).NotEmpty().WithMessage("SpecId is required.");
        RuleFor(x => x.EntityId).NotEmpty().WithMessage("EntityId is required.");
        RuleFor(x => x.TenantId).Must(NotEmptyId).WithMessage("TenantId is required.");
        RuleFor(x => x.ProjectId).Must(NotEmptyId).WithMessage("ProjectId is required.");
    }

    private static bool NotEmptyId(GlobalUniqueId id) => !id.IsEmpty;
}

/// <summary>
/// Rules shared by the create and update validators, and phrased so the message tells an author
/// how to fix the value rather than only that it is wrong.
/// </summary>
internal static class MetadataIdentifierRules
{
    internal const string TableNameMessage =
        "TableName must be a usable SQL name: letters, digits and underscores only, starting with "
        + "a letter or underscore, at most 63 bytes.";

    internal const string ColumnNameMessage =
        "ColumnName must be a usable SQL name: letters, digits and underscores only, starting with "
        + "a letter or underscore, at most 63 bytes.";

    internal static bool IsUsableSqlName(string? value) => SqlIdentifier.TryParse(value, out _);
}

public sealed class EntityFieldInputDtoValidator : AbstractValidator<EntityFieldInputDto>
{
    public EntityFieldInputDtoValidator()
    {
        RuleFor(x => x.ColumnName)
            .Must(MetadataIdentifierRules.IsUsableSqlName)
            .WithMessage(MetadataIdentifierRules.ColumnNameMessage);

        // A default is written into DDL as a literal of its column's type. Anything that is not
        // such a literal — an apostrophe that would end the string, an expression, a number that
        // is not a number — is refused rather than repaired.
        RuleFor(x => x.DefaultValue)
            .Must((field, value) => SqlDefaultValue.TryRender(value, field.FieldType, out _))
            .WithMessage(field =>
                $"DefaultValue '{field.DefaultValue}' is not a valid {field.FieldType} literal. "
                + "SQL expressions are not accepted (except NOW() for DateTime).")
            .When(x => !string.IsNullOrWhiteSpace(x.DefaultValue));
    }
}
