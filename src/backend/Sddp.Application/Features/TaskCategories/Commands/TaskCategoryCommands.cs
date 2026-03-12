using MediatR;
using Sddp.Abstractions.DTOs;
using Sddp.Abstractions.Exceptions;
using Sddp.Abstractions.Interfaces;
using Sddp.Abstractions.ValueObjects;
using Sddp.Application.Requests;
using Sddp.Domain.Entities;

namespace Sddp.Application.Features.TaskCategories.Commands;

/// <summary>
/// task create
/// </summary>
public sealed record CreateTaskCategoryCommand(
    GlobalUniqueId TenantId,
    GlobalUniqueId UserId,
    CreateTaskCategoryDto Dto) : ICommand<TaskCategoryDto?>;

public sealed class CreateTaskCategoryCommandHandler
    : IRequestHandler<CreateTaskCategoryCommand, TaskCategoryDto?>
{
    private readonly IUnitOfWork _unitOfWork;

    public CreateTaskCategoryCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<TaskCategoryDto?> Handle(
        CreateTaskCategoryCommand request, CancellationToken cancellationToken)
    {
        var repo = _unitOfWork.Repository<TaskCategory>();

        //
        var exists = await (repo.CountAsync(
            c => c.TenantId == request.TenantId
                && c.UserId == request.UserId
                && c.Name == request.Dto.Name.Trim()
                && c.IsActive,
            cancellationToken)).ConfigureAwait(false);

        if (exists > 0)
        {
            throw new SddpException("VALIDATION_ERROR", $"Category '{request.Dto.Name}' already exists");
        }

        var category = new TaskCategory(
            request.TenantId,
            request.UserId,
            request.Dto.Name,
            request.Dto.Icon,
            request.Dto.SortOrder);

        await (repo.AddAsync(category, cancellationToken)).ConfigureAwait(false);
        await (_unitOfWork.SaveChangesAsync(cancellationToken)).ConfigureAwait(false);

        return TaskCategoryMapping.MapToDto(category);
    }
}

/// <summary>
/// task update
/// </summary>
public sealed record UpdateTaskCategoryCommand(
    GlobalUniqueId TenantId,
    GlobalUniqueId UserId,
    GlobalUniqueId CategoryId,
    UpdateTaskCategoryDto Dto) : ICommand<TaskCategoryDto?>;

public sealed class UpdateTaskCategoryCommandHandler
    : IRequestHandler<UpdateTaskCategoryCommand, TaskCategoryDto?>
{
    private readonly IUnitOfWork _unitOfWork;

    public UpdateTaskCategoryCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<TaskCategoryDto?> Handle(
        UpdateTaskCategoryCommand request, CancellationToken cancellationToken)
    {
        var repo = _unitOfWork.Repository<TaskCategory>();

        var category = await (repo.GetByIdAsync(request.CategoryId, cancellationToken)).ConfigureAwait(false);
        if (category is null || !category.IsActive)
            return null;

        //
        if (category.UserId != request.UserId)
        {
            throw new SddpException("FORBIDDEN", "Cannot update another user's category");
        }

        // change
        if (request.Dto.Name is not null && request.Dto.Name.Trim() != category.Name)
        {
            var exists = await (repo.CountAsync(
                c => c.TenantId == request.TenantId
                    && c.UserId == request.UserId
                    && c.Name == request.Dto.Name.Trim()
                    && c.IsActive
                    && c.Id != request.CategoryId,
                cancellationToken)).ConfigureAwait(false);

            if (exists > 0)
            {
                throw new SddpException("VALIDATION_ERROR", $"Category '{request.Dto.Name}' already exists");
            }
        }

        category.Update(request.Dto.Name, request.Dto.Icon, request.Dto.SortOrder);
        await (repo.UpdateAsync(category, cancellationToken)).ConfigureAwait(false);
        await (_unitOfWork.SaveChangesAsync(cancellationToken)).ConfigureAwait(false);

        return TaskCategoryMapping.MapToDto(category);
    }
}

/// <summary>
/// task delete
/// </summary>
public sealed record DeleteTaskCategoryCommand(
    GlobalUniqueId TenantId,
    GlobalUniqueId UserId,
    GlobalUniqueId CategoryId) : ICommand<bool>;

public sealed class DeleteTaskCategoryCommandHandler
    : IRequestHandler<DeleteTaskCategoryCommand, bool>
{
    private readonly IUnitOfWork _unitOfWork;

    public DeleteTaskCategoryCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<bool> Handle(
        DeleteTaskCategoryCommand request, CancellationToken cancellationToken)
    {
        var repo = _unitOfWork.Repository<TaskCategory>();

        var category = await (repo.GetByIdAsync(request.CategoryId, cancellationToken)).ConfigureAwait(false);
        if (category is null || !category.IsActive)
            return false;

        //
        if (category.UserId != request.UserId)
        {
            throw new SddpException("FORBIDDEN", "Cannot delete another user's category");
        }

        category.Deactivate();
        await (repo.UpdateAsync(category, cancellationToken)).ConfigureAwait(false);
        await (_unitOfWork.SaveChangesAsync(cancellationToken)).ConfigureAwait(false);

        return true;
    }
}

/// <summary>
/// task change
/// </summary>
public sealed record ReorderTaskCategoriesCommand(
    GlobalUniqueId TenantId,
    GlobalUniqueId UserId,
    ReorderTaskCategoriesDto Dto) : ICommand<bool>;

public sealed class ReorderTaskCategoriesCommandHandler
    : IRequestHandler<ReorderTaskCategoriesCommand, bool>
{
    private readonly IUnitOfWork _unitOfWork;

    public ReorderTaskCategoriesCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<bool> Handle(
        ReorderTaskCategoriesCommand request, CancellationToken cancellationToken)
    {
        var repo = _unitOfWork.Repository<TaskCategory>();

        foreach (var item in request.Dto.Items)
        {
            if (!GlobalUniqueId.TryParse(item.Id, out var categoryId))
                continue;

            var category = await (repo.GetByIdAsync(categoryId, cancellationToken)).ConfigureAwait(false);
            if (category is null || !category.IsActive || category.UserId != request.UserId)
                continue;

            category.Update(sortOrder: item.SortOrder);
            await (repo.UpdateAsync(category, cancellationToken)).ConfigureAwait(false);
        }

        await (_unitOfWork.SaveChangesAsync(cancellationToken)).ConfigureAwait(false);
        return true;
    }
}
