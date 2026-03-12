namespace Sddp.Abstractions.DTOs;

public record CreateTaskCategoryDto(
    string Name,
    string? Icon = null,
    int SortOrder = 0);

public record UpdateTaskCategoryDto(
    string? Name = null,
    string? Icon = null,
    int? SortOrder = null);

public record TaskCategoryDto(
    string Id,
    string Name,
    string? Icon,
    int SortOrder,
    DateTimeOffset CreatedAt);

public record ReorderTaskCategoriesDto(
    IEnumerable<ReorderItemDto> Items);

public record ReorderItemDto(
    string Id,
    int SortOrder);
