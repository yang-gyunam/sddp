using MediatR;
using Sddp.Abstractions.Exceptions;
using Sddp.Abstractions.Interfaces;
using Sddp.Abstractions.ValueObjects;
using Sddp.Domain.Entities;

namespace Sddp.Application.Behaviors;

/// <summary>A command which persists a file path as a project relationship.</summary>
public interface IRepositoryBoundRequest
{
    GlobalUniqueId TenantId { get; }
    GlobalUniqueId ProjectId { get; }
}

/// <summary>Runs inside the normal command transaction, before any relationship is saved.</summary>
public sealed class RepositoryConfirmationBehavior<TRequest, TResponse>(IUnitOfWork unitOfWork)
    : IPipelineBehavior<TRequest, TResponse> where TRequest : notnull
{
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken ct)
    {
        if (request is IRepositoryBoundRequest bound)
        {
            var project = await unitOfWork.Repository<Project>().GetByIdAsync(bound.ProjectId, ct);
            if (project is null || project.TenantId != bound.TenantId || !project.IsActive)
                throw new NotFoundException("Project", bound.ProjectId.ToString());
            project.RequireConfirmedRepository();
        }
        return await next();
    }
}
