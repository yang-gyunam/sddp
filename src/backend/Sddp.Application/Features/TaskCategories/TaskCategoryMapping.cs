using Sddp.Abstractions.DTOs;
using Sddp.Domain.Entities;

namespace Sddp.Application.Features.TaskCategories;

internal static class TaskCategoryMapping
{
    internal static TaskCategoryDto MapToDto(TaskCategory category)
    {
        return new TaskCategoryDto(
            Id: category.Id.ToString(),
            Name: category.Name,
            Icon: category.Icon,
            SortOrder: category.SortOrder,
            CreatedAt: category.CreatedAt.ToDateTimeOffset());
    }
}
