using Sddp.Abstractions.DTOs;
using EntityMetadataEntity = Sddp.Domain.Entities.EntityMetadata;
using Sddp.Domain.Entities;

namespace Sddp.Application.Features.EntityMetadata;

internal static class EntityMetadataMapping
{
    internal static EntityMetadataDto MapEntity(EntityMetadataEntity entity, IReadOnlyList<FieldMetadata> fields)
    {
        var fieldDtos = fields
            .Where(f => f.IsActive)
            .OrderBy(f => f.DisplayOrder)
            .Select(MapField)
            .ToList();

        return new EntityMetadataDto(
            entity.Id.ToString(),
            entity.SpecId.ToString(),
            entity.EntityName,
            entity.TableName,
            entity.Namespace,
            entity.Description,
            entity.BaseClass,
            entity.IsGenerated,
            entity.CreatedAt.ToDateTimeOffset(),
            entity.UpdatedAt.ToDateTimeOffset(),
            fieldDtos);
    }

    internal static EntityFieldDto MapField(FieldMetadata field)
    {
        return new EntityFieldDto(
            field.Id.ToString(),
            field.FieldName,
            field.ColumnName,
            field.FieldType.ToString(),
            field.IsRequired,
            field.IsUnique,
            field.MaxLength,
            field.MinLength,
            field.ValidationType,
            field.Pattern,
            field.DefaultValue,
            field.Description,
            field.DisplayOrder);
    }
}
