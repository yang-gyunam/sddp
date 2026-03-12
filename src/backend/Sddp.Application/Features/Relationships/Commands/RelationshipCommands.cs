using MediatR;
using Sddp.Abstractions.DTOs;
using Sddp.Abstractions.Enums;
using Sddp.Abstractions.Exceptions;
using Sddp.Abstractions.Interfaces;
using Sddp.Abstractions.ValueObjects;
using Sddp.Application.Features.Relationships;
using Sddp.Application.Requests;
using Sddp.Domain.Entities;

namespace Sddp.Application.Features.Relationships.Commands;

/// <summary>
/// relationship create
/// </summary>
public sealed record CreateRelationshipCommand(
    GlobalUniqueId TenantId,
    GlobalUniqueId ProjectId,
    GlobalUniqueId UserId,
    CreateRelationshipDto Dto) : ICommand<RelationshipDto?>, IAuditableRequest<RelationshipDto?>
{
    public AuditLogRequest? AuditLog { get; set; }
    public AuditLogRequest? GetAuditLogRequest(RelationshipDto? response) => AuditLog;
}

public sealed class CreateRelationshipCommandHandler : IRequestHandler<CreateRelationshipCommand, RelationshipDto?>
{
    private readonly IUnitOfWork _unitOfWork;

    public CreateRelationshipCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<RelationshipDto?> Handle(CreateRelationshipCommand request, CancellationToken cancellationToken)
    {
        if (!Enum.TryParse<EntityType>(request.Dto.FromEntityType, out var fromEntityType))
        {
            throw new SddpException("VALIDATION_ERROR", $"Invalid FromEntityType: {request.Dto.FromEntityType}");
        }
        if (!Enum.TryParse<EntityType>(request.Dto.ToEntityType, out var toEntityType))
        {
            throw new SddpException("VALIDATION_ERROR", $"Invalid ToEntityType: {request.Dto.ToEntityType}");
        }
        if (!Enum.TryParse<RelationType>(request.Dto.Type, out var relationType))
        {
            throw new SddpException("VALIDATION_ERROR", $"Invalid RelationType: {request.Dto.Type}");
        }

        if (!GlobalUniqueId.TryParse(request.Dto.FromEntityId, out var fromEntityId))
        {
            throw new SddpException("VALIDATION_ERROR", "Invalid FromEntityId");
        }
        if (!GlobalUniqueId.TryParse(request.Dto.ToEntityId, out var toEntityId))
        {
            throw new SddpException("VALIDATION_ERROR", "Invalid ToEntityId");
        }

        var validation = await (RelationshipValidation.ValidateAsync(
            _unitOfWork,
            request.TenantId,
            request.ProjectId,
            fromEntityType,
            fromEntityId,
            toEntityType,
            toEntityId,
            relationType,
            cancellationToken)).ConfigureAwait(false);

        if (!validation.IsValid)
        {
            throw new SddpException("VALIDATION_ERROR", validation.ErrorMessage ?? "Relationship validation failed");
        }

        GlobalUniqueId? sourceSpecId = null;
        if (!string.IsNullOrEmpty(request.Dto.SourceSpecId)
            && GlobalUniqueId.TryParse(request.Dto.SourceSpecId, out var parsedSourceSpecId))
        {
            sourceSpecId = parsedSourceSpecId;
        }

        GlobalUniqueId? sourceDecisionId = null;
        if (!string.IsNullOrEmpty(request.Dto.SourceDecisionId)
            && GlobalUniqueId.TryParse(request.Dto.SourceDecisionId, out var parsedSourceDecisionId))
        {
            sourceDecisionId = parsedSourceDecisionId;
        }

        var relationship = new Relationship(
            tenantId: request.TenantId,
            projectId: request.ProjectId,
            fromEntityType: fromEntityType,
            fromEntityId: fromEntityId,
            toEntityType: toEntityType,
            toEntityId: toEntityId,
            type: relationType,
            createdBy: request.UserId,
            reason: request.Dto.Reason,
            sourceSpecId: sourceSpecId,
            sourceDecisionId: sourceDecisionId);

        var repo = _unitOfWork.Repository<Relationship>();
        await (repo.AddAsync(relationship, cancellationToken)).ConfigureAwait(false);
        await (_unitOfWork.SaveChangesAsync(cancellationToken)).ConfigureAwait(false);

        request.AuditLog = new AuditLogRequest(
            request.UserId,
            "create",
            "relationship",
            relationship.Id,
            new { request.Dto.FromEntityType, request.Dto.ToEntityType, request.Dto.Type },
            request.TenantId,
            request.ProjectId);

        return RelationshipMapping.MapToDto(relationship);
    }
}

/// <summary>
/// relationship
/// </summary>
public sealed record InvalidateRelationshipCommand(
    GlobalUniqueId TenantId,
    GlobalUniqueId ProjectId,
    GlobalUniqueId RelationshipId) : ICommand<bool>, IAuditableRequest<bool>
{
    public AuditLogRequest? AuditLog { get; set; }
    public AuditLogRequest? GetAuditLogRequest(bool response) => response ? AuditLog : null;
}

public sealed class InvalidateRelationshipCommandHandler : IRequestHandler<InvalidateRelationshipCommand, bool>
{
    private readonly IUnitOfWork _unitOfWork;

    public InvalidateRelationshipCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<bool> Handle(InvalidateRelationshipCommand request, CancellationToken cancellationToken)
    {
        var repo = _unitOfWork.Repository<Relationship>();

        var relationship = await (repo.GetByIdAsync(request.RelationshipId, cancellationToken)).ConfigureAwait(false);
        if (relationship is null
            || relationship.TenantId != request.TenantId
            || relationship.ProjectId != request.ProjectId
            || !relationship.IsActive)
        {
            return false;
        }

        if (relationship.ValidTo != null)
        {
            return false;
        }

        relationship.Invalidate();
        await (repo.UpdateAsync(relationship, cancellationToken)).ConfigureAwait(false);
        await (_unitOfWork.SaveChangesAsync(cancellationToken)).ConfigureAwait(false);

        request.AuditLog = new AuditLogRequest(
            null,
            "invalidate",
            "relationship",
            request.RelationshipId,
            null,
            request.TenantId,
            request.ProjectId);

        return true;
    }
}
