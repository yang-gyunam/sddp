using MediatR;
using Sddp.Abstractions.DTOs;
using Sddp.Abstractions.Exceptions;
using Sddp.Abstractions.Interfaces;
using Sddp.Abstractions.ValueObjects;
using Sddp.Application.Requests;
using Sddp.Domain.Entities;
using Sddp.Domain.Enums;
using EntityMetadataEntity = Sddp.Domain.Entities.EntityMetadata;

namespace Sddp.Application.Features.EntityMetadata.Commands;

/// <summary>
/// entity create
/// </summary>
public sealed record CreateEntityMetadataCommand(
    Guid SpecId,
    GlobalUniqueId TenantId,
    GlobalUniqueId ProjectId,
    CreateEntityMetadataDto Dto) : ICommand<EntityMetadataDto?>, IAuditableRequest<EntityMetadataDto?>
{
    public AuditLogRequest? AuditLog { get; set; }
    public AuditLogRequest? GetAuditLogRequest(EntityMetadataDto? response) => AuditLog;
}

public sealed class CreateEntityMetadataCommandHandler : IRequestHandler<CreateEntityMetadataCommand, EntityMetadataDto?>
{
    private readonly IUnitOfWork _unitOfWork;

    public CreateEntityMetadataCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<EntityMetadataDto?> Handle(CreateEntityMetadataCommand request, CancellationToken cancellationToken)
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

        var entity = new EntityMetadataEntity(
            request.TenantId,
            request.ProjectId,
            specGid,
            request.Dto.EntityName,
            request.Dto.TableName,
            request.Dto.Description ?? string.Empty,
            request.Dto.Namespace ?? "Sddp.Domain.Entities",
            request.Dto.BaseClass ?? "AuditableEntityBase");

        entity.SetGenerationEnabled(request.Dto.IsGenerated);

        var entityRepo = _unitOfWork.Repository<EntityMetadataEntity>();
        await (entityRepo.AddAsync(entity, cancellationToken)).ConfigureAwait(false);

        var fieldRepo = _unitOfWork.Repository<FieldMetadata>();
        var fields = new List<FieldMetadata>();

        foreach (var fieldDto in request.Dto.Fields ?? new List<EntityFieldInputDto>())
        {
            if (!EntityMetadataFieldTypeParser.TryParseFieldType(fieldDto.FieldType, out var fieldType))
            {
                throw new SddpException("INVALID_FIELD_TYPE", $"Invalid FieldType: {fieldDto.FieldType}");
            }

            var field = new FieldMetadata(
                entity.Id,
                fieldDto.FieldName,
                fieldDto.ColumnName,
                fieldType,
                fieldDto.IsRequired,
                fieldDto.IsUnique,
                fieldDto.MaxLength,
                fieldDto.MinLength,
                fieldDto.ValidationType,
                fieldDto.Pattern,
                fieldDto.DefaultValue,
                fieldDto.Description ?? string.Empty,
                fieldDto.DisplayOrder);
            await (fieldRepo.AddAsync(field, cancellationToken)).ConfigureAwait(false);
            fields.Add(field);
        }

        await (_unitOfWork.SaveChangesAsync(cancellationToken)).ConfigureAwait(false);

        request.AuditLog = new AuditLogRequest(
            null,
            "create",
            "entity_metadata",
            entity.Id,
            new { entity.EntityName, entity.TableName },
            request.TenantId,
            request.ProjectId);

        return EntityMetadataMapping.MapEntity(entity, fields);
    }
}

/// <summary>
/// entity update
/// </summary>
public sealed record UpdateEntityMetadataCommand(
    Guid SpecId,
    Guid EntityId,
    GlobalUniqueId TenantId,
    GlobalUniqueId ProjectId,
    UpdateEntityMetadataDto Dto) : ICommand<EntityMetadataDto?>, IAuditableRequest<EntityMetadataDto?>
{
    public AuditLogRequest? AuditLog { get; set; }
    public AuditLogRequest? GetAuditLogRequest(EntityMetadataDto? response) => AuditLog;
}

public sealed class UpdateEntityMetadataCommandHandler : IRequestHandler<UpdateEntityMetadataCommand, EntityMetadataDto?>
{
    private readonly IUnitOfWork _unitOfWork;

    public UpdateEntityMetadataCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<EntityMetadataDto?> Handle(UpdateEntityMetadataCommand request, CancellationToken cancellationToken)
    {
        var specGid = GlobalUniqueId.FromGuid(request.SpecId);
        var entityGid = GlobalUniqueId.FromGuid(request.EntityId);

        var entityRepo = _unitOfWork.Repository<EntityMetadataEntity>();
        var entities = await (entityRepo.FindAsync(
            e => e.Id == entityGid
                 && e.SpecId == specGid
                 && e.TenantId == request.TenantId
                 && e.ProjectId == request.ProjectId,
            cancellationToken)).ConfigureAwait(false);

        var entity = entities.FirstOrDefault();
        if (entity is null)
        {
            return null;
        }

        entity.Update(
            request.Dto.EntityName,
            request.Dto.TableName,
            request.Dto.Description ?? string.Empty,
            request.Dto.Namespace ?? "Sddp.Domain.Entities",
            request.Dto.BaseClass ?? "AuditableEntityBase");

        entity.SetGenerationEnabled(request.Dto.IsGenerated);
        await (entityRepo.UpdateAsync(entity, cancellationToken)).ConfigureAwait(false);

        var fieldRepo = _unitOfWork.Repository<FieldMetadata>();
        var existingFields = await (fieldRepo.FindAsync(
            f => f.EntityMetadataId == entity.Id,
            cancellationToken)).ConfigureAwait(false);

        var existingFieldsById = existingFields.ToDictionary(f => f.Id.ToString(), f => f);
        var incomingIds = new HashSet<string>();

        foreach (var fieldDto in request.Dto.Fields ?? new List<EntityFieldInputDto>())
        {
            if (!EntityMetadataFieldTypeParser.TryParseFieldType(fieldDto.FieldType, out var fieldType))
            {
                throw new SddpException("INVALID_FIELD_TYPE", $"Invalid FieldType: {fieldDto.FieldType}");
            }

            if (!string.IsNullOrWhiteSpace(fieldDto.Id) && existingFieldsById.TryGetValue(fieldDto.Id, out var existingField))
            {
                existingField.Update(
                    fieldDto.FieldName,
                    fieldDto.ColumnName,
                    fieldType,
                    fieldDto.IsRequired,
                    fieldDto.IsUnique,
                    fieldDto.MaxLength,
                    fieldDto.MinLength,
                    fieldDto.ValidationType,
                    fieldDto.Pattern,
                    fieldDto.DefaultValue,
                    fieldDto.Description ?? string.Empty,
                    fieldDto.DisplayOrder);
                await (fieldRepo.UpdateAsync(existingField, cancellationToken)).ConfigureAwait(false);
                incomingIds.Add(existingField.Id.ToString());
            }
            else
            {
                var field = new FieldMetadata(
                    entity.Id,
                    fieldDto.FieldName,
                    fieldDto.ColumnName,
                    fieldType,
                    fieldDto.IsRequired,
                    fieldDto.IsUnique,
                    fieldDto.MaxLength,
                    fieldDto.MinLength,
                    fieldDto.ValidationType,
                    fieldDto.Pattern,
                    fieldDto.DefaultValue,
                    fieldDto.Description ?? string.Empty,
                    fieldDto.DisplayOrder);
                await (fieldRepo.AddAsync(field, cancellationToken)).ConfigureAwait(false);
            }
        }

        foreach (var existing in existingFields)
        {
            if (!incomingIds.Contains(existing.Id.ToString()))
            {
                await (fieldRepo.DeleteAsync(existing, cancellationToken)).ConfigureAwait(false);
            }
        }

        await (_unitOfWork.SaveChangesAsync(cancellationToken)).ConfigureAwait(false);

        var updatedFields = await (fieldRepo.FindAsync(
            f => f.EntityMetadataId == entity.Id && f.IsActive,
            cancellationToken)).ConfigureAwait(false);

        request.AuditLog = new AuditLogRequest(
            null,
            "update",
            "entity_metadata",
            entity.Id,
            new { entity.EntityName, entity.TableName },
            request.TenantId,
            request.ProjectId);

        return EntityMetadataMapping.MapEntity(entity, updatedFields);
    }
}

/// <summary>
/// entity delete
/// </summary>
public sealed record DeleteEntityMetadataCommand(
    Guid SpecId,
    Guid EntityId,
    GlobalUniqueId TenantId,
    GlobalUniqueId ProjectId) : ICommand<bool>, IAuditableRequest<bool>
{
    public AuditLogRequest? AuditLog { get; set; }
    public AuditLogRequest? GetAuditLogRequest(bool response) => response ? AuditLog : null;
}

public sealed class DeleteEntityMetadataCommandHandler : IRequestHandler<DeleteEntityMetadataCommand, bool>
{
    private readonly IUnitOfWork _unitOfWork;

    public DeleteEntityMetadataCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<bool> Handle(DeleteEntityMetadataCommand request, CancellationToken cancellationToken)
    {
        var specGid = GlobalUniqueId.FromGuid(request.SpecId);
        var entityGid = GlobalUniqueId.FromGuid(request.EntityId);

        var entityRepo = _unitOfWork.Repository<EntityMetadataEntity>();
        var entities = await (entityRepo.FindAsync(
            e => e.Id == entityGid
                 && e.SpecId == specGid
                 && e.TenantId == request.TenantId
                 && e.ProjectId == request.ProjectId,
            cancellationToken)).ConfigureAwait(false);

        var entity = entities.FirstOrDefault();
        if (entity is null)
        {
            return false;
        }

        await (entityRepo.DeleteAsync(entity, cancellationToken)).ConfigureAwait(false);
        await (_unitOfWork.SaveChangesAsync(cancellationToken)).ConfigureAwait(false);

        request.AuditLog = new AuditLogRequest(
            null,
            "delete",
            "entity_metadata",
            entityGid,
            null,
            request.TenantId,
            request.ProjectId);

        return true;
    }
}

internal static class EntityMetadataFieldTypeParser
{
    internal static bool TryParseFieldType(string fieldType, out FieldType result)
    {
        return Enum.TryParse(fieldType, true, out result);
    }
}
