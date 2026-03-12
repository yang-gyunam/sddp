namespace Sddp.Abstractions.DTOs;

public record EntityFieldDto(
    string Id,
    string FieldName,
    string ColumnName,
    string FieldType,
    bool IsRequired,
    bool IsUnique,
    int? MaxLength,
    int? MinLength,
    string? ValidationType,
    string? Pattern,
    string? DefaultValue,
    string Description,
    int DisplayOrder);

public record EntityMetadataDto(
    string Id,
    string SpecId,
    string EntityName,
    string TableName,
    string Namespace,
    string Description,
    string BaseClass,
    bool IsGenerated,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    List<EntityFieldDto> Fields);

public record EntityFieldInputDto(
    string? Id,
    string FieldName,
    string ColumnName,
    string FieldType,
    bool IsRequired,
    bool IsUnique,
    int? MaxLength,
    int? MinLength,
    string? ValidationType,
    string? Pattern,
    string? DefaultValue,
    string Description,
    int DisplayOrder);

public record CreateEntityMetadataDto(
    string EntityName,
    string TableName,
    string Namespace,
    string Description,
    string BaseClass,
    bool IsGenerated,
    List<EntityFieldInputDto> Fields);

public record UpdateEntityMetadataDto(
    string EntityName,
    string TableName,
    string Namespace,
    string Description,
    string BaseClass,
    bool IsGenerated,
    List<EntityFieldInputDto> Fields);
