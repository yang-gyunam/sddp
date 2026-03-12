using MediatR;
using Sddp.Abstractions.DTOs;
using Sddp.Abstractions.Interfaces;
using Sddp.Application.Requests;
using Sddp.Domain.Entities;

namespace Sddp.Application.Features.Permissions.Queries;

/// <summary>
/// all permission get
/// </summary>
public sealed record GetPermissionsQuery : IQuery<IReadOnlyList<PermissionDto>>;

public sealed class GetPermissionsQueryHandler : IRequestHandler<GetPermissionsQuery, IReadOnlyList<PermissionDto>>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetPermissionsQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<IReadOnlyList<PermissionDto>> Handle(GetPermissionsQuery request, CancellationToken cancellationToken)
    {
        var permissionRepo = _unitOfWork.Repository<Permission>();
        var permissions = await (permissionRepo.GetAllAsync(cancellationToken)).ConfigureAwait(false);

        return permissions.Select(p => new PermissionDto(
            Id: p.Id.ToString(),
            Code: p.Code,
            Name: p.Name,
            Resource: p.Resource,
            Action: p.Action,
            Description: p.Description)).ToList();
    }
}

/// <summary>
/// permission get
/// </summary>
public sealed record GetPermissionsByResourceQuery : IQuery<Dictionary<string, IEnumerable<PermissionDto>>>;

public sealed class GetPermissionsByResourceQueryHandler : IRequestHandler<GetPermissionsByResourceQuery, Dictionary<string, IEnumerable<PermissionDto>>>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetPermissionsByResourceQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<Dictionary<string, IEnumerable<PermissionDto>>> Handle(
        GetPermissionsByResourceQuery request,
        CancellationToken cancellationToken)
    {
        var permissionRepo = _unitOfWork.Repository<Permission>();
        var permissions = await (permissionRepo.GetAllAsync(cancellationToken)).ConfigureAwait(false);

        return permissions
            .GroupBy(p => p.Resource)
            .ToDictionary(
                g => g.Key,
                g => g.Select(p => new PermissionDto(
                    Id: p.Id.ToString(),
                    Code: p.Code,
                    Name: p.Name,
                    Resource: p.Resource,
                    Action: p.Action,
                    Description: p.Description)));
    }
}
