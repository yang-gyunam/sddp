using MediatR;
using Sddp.Abstractions.DTOs;
using Sddp.Abstractions.Interfaces;
using Sddp.Abstractions.ValueObjects;
using Sddp.Application.Requests;
using Sddp.Domain.Entities;
using EntityMetadataEntity = Sddp.Domain.Entities.EntityMetadata;

namespace Sddp.Application.Features.EntityMetadata.Queries;

/// <summary>
/// Spec entity get
/// </summary>
public sealed record GetEntityMetadataQuery(
    Guid SpecId,
    GlobalUniqueId TenantId,
    GlobalUniqueId ProjectId) : IQuery<IReadOnlyList<EntityMetadataDto>?>;

public sealed class GetEntityMetadataQueryHandler : IRequestHandler<GetEntityMetadataQuery, IReadOnlyList<EntityMetadataDto>?>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetEntityMetadataQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<IReadOnlyList<EntityMetadataDto>?> Handle(GetEntityMetadataQuery request, CancellationToken cancellationToken)
    {
        var specGid = GlobalUniqueId.FromGuid(request.SpecId);
        var specRepo = _unitOfWork.Repository<Spec>();
        var specExists = (await (specRepo.FindAsync(
            s => s.Id == specGid && s.TenantId == request.TenantId && s.ProjectId == request.ProjectId,
            cancellationToken)).ConfigureAwait(false)).Any();

        if (!specExists)
        {
            return null;
        }

        var entityRepo = _unitOfWork.Repository<EntityMetadataEntity>();
        var entities = await (entityRepo.FindAsync(
            e => e.SpecId == specGid
                 && e.TenantId == request.TenantId
                 && e.ProjectId == request.ProjectId
                 && e.IsActive,
            cancellationToken)).ConfigureAwait(false);

        var entityList = entities.OrderBy(e => e.EntityName).ToList();
        if (entityList.Count == 0)
        {
            return [];
        }

        var entityIds = entityList.Select(e => e.Id).ToHashSet();
        var fieldRepo = _unitOfWork.Repository<FieldMetadata>();
        var fields = await (fieldRepo.FindAsync(
            f => entityIds.Contains(f.EntityMetadataId) && f.IsActive,
            cancellationToken)).ConfigureAwait(false);

        var fieldsByEntity = fields
            .GroupBy(f => f.EntityMetadataId)
            .ToDictionary(g => g.Key, g => g.ToList());

        return entityList
            .Select(entity =>
            {
                fieldsByEntity.TryGetValue(entity.Id, out var entityFields);
                return EntityMetadataMapping.MapEntity(entity, entityFields ?? []);
            })
            .ToList();
    }
}
