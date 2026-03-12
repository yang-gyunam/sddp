using MediatR;
using Sddp.Abstractions.DTOs;
using Sddp.Abstractions.Interfaces;
using Sddp.Abstractions.ValueObjects;
using Sddp.Application.Requests;
using Sddp.Domain.Entities;

namespace Sddp.Application.Features.Artifacts.Queries;

// --- By Spec ---

/// <summary>
/// spec get
/// </summary>
public sealed record GetMappingsBySpecQuery(
    GlobalUniqueId TenantId,
    GlobalUniqueId ProjectId,
    GlobalUniqueId SpecId) : IQuery<IReadOnlyList<ArtifactToSpecMappingDto>>;

public sealed class GetMappingsBySpecQueryHandler
    : IRequestHandler<GetMappingsBySpecQuery, IReadOnlyList<ArtifactToSpecMappingDto>>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetMappingsBySpecQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<IReadOnlyList<ArtifactToSpecMappingDto>> Handle(
        GetMappingsBySpecQuery request,
        CancellationToken cancellationToken)
    {
        var repo = _unitOfWork.Repository<ArtifactToSpecMapping>();
        var mappings = await (repo.FindAsync(
            x => x.TenantId == request.TenantId
                && x.ProjectId == request.ProjectId
                && x.SpecId == request.SpecId
                && x.IsActive,
            cancellationToken)).ConfigureAwait(false);

        return mappings.Select(ArtifactMappingMapper.MapToDto).ToList();
    }
}

// --- By Path ---

/// <summary>
/// get
/// </summary>
public sealed record GetMappingsByPathQuery(
    GlobalUniqueId TenantId,
    GlobalUniqueId ProjectId,
    string Path) : IQuery<IReadOnlyList<ArtifactToSpecMappingDto>>;

public sealed class GetMappingsByPathQueryHandler
    : IRequestHandler<GetMappingsByPathQuery, IReadOnlyList<ArtifactToSpecMappingDto>>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetMappingsByPathQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<IReadOnlyList<ArtifactToSpecMappingDto>> Handle(
        GetMappingsByPathQuery request,
        CancellationToken cancellationToken)
    {
        var repo = _unitOfWork.Repository<ArtifactToSpecMapping>();
        var mappings = await (repo.FindAsync(
            x => x.TenantId == request.TenantId
                && x.ProjectId == request.ProjectId
                && x.ArtifactPath == request.Path
                && x.IsActive,
            cancellationToken)).ConfigureAwait(false);

        return mappings.Select(ArtifactMappingMapper.MapToDto).ToList();
    }
}

// --- Project All ---

/// <summary>
/// project all get
/// </summary>
public sealed record GetProjectMappingsQuery(
    GlobalUniqueId TenantId,
    GlobalUniqueId ProjectId) : IQuery<IReadOnlyList<ArtifactToSpecMappingDto>>;

public sealed class GetProjectMappingsQueryHandler
    : IRequestHandler<GetProjectMappingsQuery, IReadOnlyList<ArtifactToSpecMappingDto>>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetProjectMappingsQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<IReadOnlyList<ArtifactToSpecMappingDto>> Handle(
        GetProjectMappingsQuery request,
        CancellationToken cancellationToken)
    {
        var repo = _unitOfWork.Repository<ArtifactToSpecMapping>();
        var mappings = await (repo.FindAsync(
            x => x.TenantId == request.TenantId
                && x.ProjectId == request.ProjectId
                && x.IsActive,
            cancellationToken)).ConfigureAwait(false);

        return mappings.Select(ArtifactMappingMapper.MapToDto).ToList();
    }
}

// --- By Id ---

/// <summary>
/// get (ID)
/// </summary>
public sealed record GetMappingByIdQuery(
    GlobalUniqueId TenantId,
    GlobalUniqueId ProjectId,
    GlobalUniqueId MappingId) : IQuery<ArtifactToSpecMappingDto?>;

public sealed class GetMappingByIdQueryHandler
    : IRequestHandler<GetMappingByIdQuery, ArtifactToSpecMappingDto?>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetMappingByIdQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<ArtifactToSpecMappingDto?> Handle(
        GetMappingByIdQuery request,
        CancellationToken cancellationToken)
    {
        var repo = _unitOfWork.Repository<ArtifactToSpecMapping>();
        var mapping = await (repo.GetByIdAsync(request.MappingId, cancellationToken)).ConfigureAwait(false);

        if (mapping == null
            || mapping.TenantId != request.TenantId
            || mapping.ProjectId != request.ProjectId
            || !mapping.IsActive)
        {
            return null;
        }

        return ArtifactMappingMapper.MapToDto(mapping);
    }
}
