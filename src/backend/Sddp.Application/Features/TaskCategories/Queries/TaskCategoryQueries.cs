using MediatR;
using Sddp.Abstractions.DTOs;
using Sddp.Abstractions.Interfaces;
using Sddp.Abstractions.ValueObjects;
using Sddp.Application.Requests;
using Sddp.Domain.Entities;

namespace Sddp.Application.Features.TaskCategories.Queries;

/// <summary>
/// task get
/// </summary>
public sealed record GetTaskCategoriesQuery(
    GlobalUniqueId TenantId,
    GlobalUniqueId UserId) : IQuery<IEnumerable<TaskCategoryDto>>;

public sealed class GetTaskCategoriesQueryHandler
    : IRequestHandler<GetTaskCategoriesQuery, IEnumerable<TaskCategoryDto>>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetTaskCategoriesQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<IEnumerable<TaskCategoryDto>> Handle(
        GetTaskCategoriesQuery request, CancellationToken cancellationToken)
    {
        var repo = _unitOfWork.Repository<TaskCategory>();

        var categories = await (repo.FindAsync(
            c => c.TenantId == request.TenantId
                && c.UserId == request.UserId
                && c.IsActive,
            cancellationToken)).ConfigureAwait(false);

        return categories
            .OrderBy(c => c.SortOrder)
            .ThenBy(c => c.CreatedAt.ToDateTimeOffset())
            .Select(TaskCategoryMapping.MapToDto);
    }
}
