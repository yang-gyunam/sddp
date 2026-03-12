using MediatR;
using Sddp.Abstractions.DTOs;
using Sddp.Abstractions.Exceptions;
using Sddp.Abstractions.Interfaces;
using Sddp.Abstractions.ValueObjects;
using Sddp.Application.Requests;
using Sddp.Domain.Entities;
using Sddp.Domain.Enums;

namespace Sddp.Application.Features.Artifacts.Commands;

// --- Create ---

/// <summary>
/// -spec create
/// </summary>
public sealed record CreateArtifactMappingCommand(
    GlobalUniqueId TenantId,
    GlobalUniqueId ProjectId,
    GlobalUniqueId SpecId,
    string ArtifactPath,
    string MappingReason,
    string? SourceContent,
    string? Notes,
    GlobalUniqueId UserId) : ICommand<ArtifactToSpecMappingDto>, IAuditableRequest<ArtifactToSpecMappingDto>
{
    public AuditLogRequest? AuditLog { get; set; }
    public AuditLogRequest? GetAuditLogRequest(ArtifactToSpecMappingDto response) => AuditLog;
}

public sealed class CreateArtifactMappingCommandHandler : IRequestHandler<CreateArtifactMappingCommand, ArtifactToSpecMappingDto>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IEmbeddingTriggerService _embeddingTrigger;

    public CreateArtifactMappingCommandHandler(
        IUnitOfWork unitOfWork,
        IEmbeddingTriggerService embeddingTrigger)
    {
        _unitOfWork = unitOfWork;
        _embeddingTrigger = embeddingTrigger;
    }

    public async Task<ArtifactToSpecMappingDto> Handle(
        CreateArtifactMappingCommand request,
        CancellationToken cancellationToken)
    {
        var reason = Enum.TryParse<MappingReason>(request.MappingReason, ignoreCase: true, out var parsed)
            ? parsed
            : MappingReason.Manual;

        var mapping = new ArtifactToSpecMapping(
            request.TenantId,
            request.ProjectId,
            request.SpecId,
            request.ArtifactPath,
            reason,
            request.SourceContent,
            request.Notes);

        mapping.SetCreatedBy(request.UserId);

        var repo = _unitOfWork.Repository<ArtifactToSpecMapping>();
        await (repo.AddAsync(mapping, cancellationToken)).ConfigureAwait(false);
        await (_unitOfWork.SaveChangesAsync(cancellationToken)).ConfigureAwait(false);

        //
        if (!string.IsNullOrEmpty(request.SourceContent))
        {
            _embeddingTrigger.TriggerArtifactMappingEmbedding(
                request.TenantId.ToGuid(),
                request.ProjectId.ToGuid(),
                mapping.Id.ToGuid());
        }

        request.AuditLog = new AuditLogRequest(
            ActorId: request.UserId,
            Action: "create",
            ResourceType: "artifact_mapping",
            ResourceId: mapping.Id,
            Payload: new { request.ArtifactPath, request.MappingReason },
            TenantId: request.TenantId,
            ProjectId: request.ProjectId);

        return ArtifactMappingMapper.MapToDto(mapping);
    }
}

// --- Update Source ---

/// <summary>
/// -spec update
/// </summary>
public sealed record UpdateMappingSourceCommand(
    GlobalUniqueId TenantId,
    GlobalUniqueId ProjectId,
    GlobalUniqueId MappingId,
    string? SourceContent,
    string? Notes,
    GlobalUniqueId UserId) : ICommand<ArtifactToSpecMappingDto>, IAuditableRequest<ArtifactToSpecMappingDto>
{
    public AuditLogRequest? AuditLog { get; set; }
    public AuditLogRequest? GetAuditLogRequest(ArtifactToSpecMappingDto response) => AuditLog;
}

public sealed class UpdateMappingSourceCommandHandler : IRequestHandler<UpdateMappingSourceCommand, ArtifactToSpecMappingDto>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IEmbeddingTriggerService _embeddingTrigger;

    public UpdateMappingSourceCommandHandler(
        IUnitOfWork unitOfWork,
        IEmbeddingTriggerService embeddingTrigger)
    {
        _unitOfWork = unitOfWork;
        _embeddingTrigger = embeddingTrigger;
    }

    public async Task<ArtifactToSpecMappingDto> Handle(
        UpdateMappingSourceCommand request,
        CancellationToken cancellationToken)
    {
        var repo = _unitOfWork.Repository<ArtifactToSpecMapping>();
        var mapping = await (repo.GetByIdAsync(request.MappingId, cancellationToken)).ConfigureAwait(false);

        if (mapping == null
            || mapping.TenantId != request.TenantId
            || mapping.ProjectId != request.ProjectId
            || !mapping.IsActive)
        {
            throw new NotFoundException("ArtifactMapping", request.MappingId.ToString());
        }

        mapping.UpdateSource(request.SourceContent, request.Notes);
        mapping.SetUpdatedBy(request.UserId);
        await (repo.UpdateAsync(mapping, cancellationToken)).ConfigureAwait(false);
        await (_unitOfWork.SaveChangesAsync(cancellationToken)).ConfigureAwait(false);

        //
        if (!string.IsNullOrEmpty(request.SourceContent))
        {
            _embeddingTrigger.TriggerArtifactMappingEmbedding(
                request.TenantId.ToGuid(),
                request.ProjectId.ToGuid(),
                mapping.Id.ToGuid());
        }

        request.AuditLog = new AuditLogRequest(
            ActorId: request.UserId,
            Action: "update",
            ResourceType: "artifact_mapping",
            ResourceId: request.MappingId,
            Payload: new { mapping.ArtifactPath, mapping.SpecId },
            TenantId: request.TenantId,
            ProjectId: request.ProjectId);

        return ArtifactMappingMapper.MapToDto(mapping);
    }
}

// --- Delete (Soft) ---

/// <summary>
/// -spec delete (delete)
/// </summary>
public sealed record DeleteArtifactMappingCommand(
    GlobalUniqueId TenantId,
    GlobalUniqueId ProjectId,
    GlobalUniqueId MappingId,
    GlobalUniqueId UserId) : ICommand<bool>, IAuditableRequest<bool>
{
    public AuditLogRequest? AuditLog { get; set; }
    public AuditLogRequest? GetAuditLogRequest(bool response) => AuditLog;
}

public sealed class DeleteArtifactMappingCommandHandler : IRequestHandler<DeleteArtifactMappingCommand, bool>
{
    private readonly IUnitOfWork _unitOfWork;

    public DeleteArtifactMappingCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<bool> Handle(
        DeleteArtifactMappingCommand request,
        CancellationToken cancellationToken)
    {
        var repo = _unitOfWork.Repository<ArtifactToSpecMapping>();
        var mapping = await (repo.GetByIdAsync(request.MappingId, cancellationToken)).ConfigureAwait(false);

        if (mapping == null
            || mapping.TenantId != request.TenantId
            || mapping.ProjectId != request.ProjectId
            || !mapping.IsActive)
        {
            return false;
        }

        mapping.SetUpdatedBy(request.UserId);
        await (repo.DeleteAsync(mapping, cancellationToken)).ConfigureAwait(false);
        await (_unitOfWork.SaveChangesAsync(cancellationToken)).ConfigureAwait(false);

        request.AuditLog = new AuditLogRequest(
            ActorId: request.UserId,
            Action: "delete",
            ResourceType: "artifact_mapping",
            ResourceId: request.MappingId,
            Payload: new { mapping.ArtifactPath, mapping.SpecId, MappingReason = mapping.MappingReason.ToString() },
            TenantId: request.TenantId,
            ProjectId: request.ProjectId);

        return true;
    }
}
