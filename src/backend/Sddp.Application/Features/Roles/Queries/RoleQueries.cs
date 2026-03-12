using MediatR;
using Sddp.Abstractions.DTOs;
using Sddp.Abstractions.Interfaces;
using Sddp.Abstractions.ValueObjects;
using Sddp.Application.Requests;
using Sddp.Domain.Entities;

namespace Sddp.Application.Features.Roles.Queries;

/// <summary>
/// role get
/// </summary>
public sealed record GetRolesQuery : IQuery<IReadOnlyList<RoleDto>>;

public sealed class GetRolesQueryHandler : IRequestHandler<GetRolesQuery, IReadOnlyList<RoleDto>>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetRolesQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<IReadOnlyList<RoleDto>> Handle(GetRolesQuery request, CancellationToken cancellationToken)
    {
        var roleRepo = _unitOfWork.Repository<Role>();
        var rolePermissionRepo = _unitOfWork.Repository<RolePermission>();
        var permissionRepo = _unitOfWork.Repository<Permission>();

        var roles = await (roleRepo.GetAllAsync(cancellationToken)).ConfigureAwait(false);
        var rolePermissions = await (rolePermissionRepo.GetAllAsync(cancellationToken)).ConfigureAwait(false);
        var permissions = await (permissionRepo.GetAllAsync(cancellationToken)).ConfigureAwait(false);

        return roles.Select(role =>
        {
            var permissionIds = rolePermissions
                .Where(rp => rp.RoleId == role.Id)
                .Select(rp => rp.PermissionId)
                .ToList();

            var rolePermissionCodes = permissions
                .Where(p => permissionIds.Contains(p.Id))
                .Select(p => p.Code);

            return RoleMapping.MapToDto(role, rolePermissionCodes);
        }).ToList();
    }
}

/// <summary>
/// role get (ID)
/// </summary>
public sealed record GetRoleByIdQuery(GlobalUniqueId RoleId) : IQuery<RoleDto?>;

public sealed class GetRoleByIdQueryHandler : IRequestHandler<GetRoleByIdQuery, RoleDto?>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetRoleByIdQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<RoleDto?> Handle(GetRoleByIdQuery request, CancellationToken cancellationToken)
    {
        var roleRepo = _unitOfWork.Repository<Role>();
        var rolePermissionRepo = _unitOfWork.Repository<RolePermission>();
        var permissionRepo = _unitOfWork.Repository<Permission>();

        var role = await (roleRepo.GetByIdAsync(request.RoleId, cancellationToken)).ConfigureAwait(false);
        if (role is null)
        {
            return null;
        }

        var rolePermissions = await (rolePermissionRepo.FindAsync(rp => rp.RoleId == request.RoleId, cancellationToken)).ConfigureAwait(false);
        var permissionIds = rolePermissions.Select(rp => rp.PermissionId).ToList();

        var permissions = await (permissionRepo.GetAllAsync(cancellationToken)).ConfigureAwait(false);
        var rolePermissionCodes = permissions
            .Where(p => permissionIds.Contains(p.Id))
            .Select(p => p.Code);

        return RoleMapping.MapToDto(role, rolePermissionCodes);
    }
}
