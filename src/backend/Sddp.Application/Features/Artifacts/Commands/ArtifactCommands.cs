using Sddp.Application.Behaviors;
using MediatR;
using Sddp.Abstractions.DTOs;
using Sddp.Abstractions.Exceptions;
using Sddp.Abstractions.Interfaces;
using Sddp.Abstractions.ValueObjects;
using Sddp.Application.Features.Artifacts;
using Sddp.Application.Requests;
using Sddp.Domain.Entities;
using Sddp.Domain.Enums;

namespace Sddp.Application.Features.Artifacts.Commands;

/// <summary>
/// create (Upsert)
/// </summary>
public sealed record UpsertArtifactCommand(
    GlobalUniqueId TenantId,
    GlobalUniqueId ProjectId,
    GlobalUniqueId UserId,
    GlobalUniqueId SpecId,
    string ArtifactPath,
    string ArtifactType,
    string ContentHash,
    string GeneratorVersion,
    string TemplateVersion,
    string EntityName,
    GlobalUniqueId? GlossaryTermId = null,
    GlobalUniqueId? SourceConversationId = null,
    GlobalUniqueId? SourceRequirementId = null,
    GlobalUniqueId? OwnerUserId = null,
    GlobalUniqueId? ArtifactId = null)
    : ICommand<ArtifactTrackingDto>,
      IAuditableRequest<ArtifactTrackingDto>,
      IProducesArtifactDescriptor<ArtifactTrackingDto>, IRepositoryBoundRequest
{
    public AuditLogRequest? AuditLog { get; set; }
    public AuditLogRequest? GetAuditLogRequest(ArtifactTrackingDto response) => AuditLog;

    public GlobalUniqueId? GetDescribedArtifactId(ArtifactTrackingDto response) =>
        GlobalUniqueId.TryParse(response?.Id, out var id) ? id : null;
}

public sealed class UpsertArtifactCommandHandler : IRequestHandler<UpsertArtifactCommand, ArtifactTrackingDto>
{
    private readonly IUnitOfWork _unitOfWork;

    public UpsertArtifactCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<ArtifactTrackingDto> Handle(UpsertArtifactCommand request, CancellationToken cancellationToken)
    {
        var repo = _unitOfWork.Repository<ArtifactTracking>();

        ArtifactTracking? artifact = null;

        // Edit mode: find by ID
        if (request.ArtifactId.HasValue)
        {
            artifact = await (repo.GetByIdAsync(request.ArtifactId.Value, cancellationToken)).ConfigureAwait(false);
            if (artifact == null || artifact.TenantId != request.TenantId
                || artifact.ProjectId != request.ProjectId || !artifact.IsActive)
            {
                artifact = null;
            }
        }

        // Create/upsert mode: find by natural key (path + specId)
        if (artifact == null)
        {
            var existing = await (repo.FindAsync(
                x => x.TenantId == request.TenantId
                    && x.ProjectId == request.ProjectId
                    && x.ArtifactPath == request.ArtifactPath
                    && x.SpecId == request.SpecId
                    && x.IsActive,
                cancellationToken)).ConfigureAwait(false);
            artifact = existing.FirstOrDefault();
        }

        if (artifact != null)
        {
            artifact.UpdateHash(request.ContentHash, request.GeneratorVersion, request.TemplateVersion);
            artifact.SetUpdatedBy(request.UserId);
            if (request.ArtifactPath != artifact.ArtifactPath)
            {
                artifact.UpdatePath(request.ArtifactPath);
            }
            var parsedType = Enum.Parse<ArtifactType>(request.ArtifactType, ignoreCase: true);
            if (parsedType != artifact.ArtifactType)
            {
                artifact.UpdateArtifactType(parsedType);
            }
            if (!string.IsNullOrWhiteSpace(request.EntityName) && request.EntityName != artifact.EntityName)
            {
                artifact.UpdateEntityName(request.EntityName);
            }
            if (request.GlossaryTermId != artifact.GlossaryTermId)
            {
                artifact.SetGlossaryTerm(request.GlossaryTermId);
            }
            if (request.SourceConversationId != artifact.SourceConversationId)
            {
                artifact.SetSourceConversation(request.SourceConversationId);
            }
            if (request.SourceRequirementId != artifact.SourceRequirementId)
            {
                artifact.SetSourceRequirement(request.SourceRequirementId);
            }
            if (request.OwnerUserId != artifact.OwnerUserId)
            {
                artifact.SetOwner(request.OwnerUserId);
            }
            await (repo.UpdateAsync(artifact, cancellationToken)).ConfigureAwait(false);
        }
        else
        {
            var parsedType = Enum.Parse<ArtifactType>(request.ArtifactType, ignoreCase: true);
            artifact = new ArtifactTracking(
                request.TenantId,
                request.ProjectId,
                request.SpecId,
                request.ArtifactPath,
                parsedType,
                request.ContentHash,
                request.GeneratorVersion,
                request.TemplateVersion,
                request.EntityName);
            artifact.SetCreatedBy(request.UserId);
            if (request.GlossaryTermId.HasValue)
            {
                artifact.SetGlossaryTerm(request.GlossaryTermId);
            }
            if (request.SourceConversationId.HasValue)
            {
                artifact.SetSourceConversation(request.SourceConversationId);
            }
            if (request.SourceRequirementId.HasValue)
            {
                artifact.SetSourceRequirement(request.SourceRequirementId);
            }
            if (request.OwnerUserId.HasValue)
            {
                artifact.SetOwner(request.OwnerUserId);
            }
            await (repo.AddAsync(artifact, cancellationToken)).ConfigureAwait(false);
        }

        await (_unitOfWork.SaveChangesAsync(cancellationToken)).ConfigureAwait(false);

        request.AuditLog = new AuditLogRequest(
            ActorId: null,
            Action: "upsert",
            ResourceType: "artifact",
            ResourceId: artifact.Id,
            Payload: new { request.ArtifactPath, request.ArtifactType },
            TenantId: request.TenantId,
            ProjectId: request.ProjectId);

        return ArtifactMapping.MapToDto(artifact);
    }
}


public sealed record VerifyArtifactCommand(
    GlobalUniqueId TenantId,
    GlobalUniqueId ProjectId,
    GlobalUniqueId ArtifactId,
    string CurrentHash) : ICommand<ArtifactVerifyResult>, IAuditableRequest<ArtifactVerifyResult>
{
    public AuditLogRequest? AuditLog { get; set; }
    public AuditLogRequest? GetAuditLogRequest(ArtifactVerifyResult response) => AuditLog;
}

public sealed class VerifyArtifactCommandHandler : IRequestHandler<VerifyArtifactCommand, ArtifactVerifyResult>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IArtifactRepositoryService _artifactRepositoryService;

    public VerifyArtifactCommandHandler(
        IUnitOfWork unitOfWork,
        IArtifactRepositoryService artifactRepositoryService)
    {
        _unitOfWork = unitOfWork;
        _artifactRepositoryService = artifactRepositoryService;
    }

    public async Task<ArtifactVerifyResult> Handle(VerifyArtifactCommand request, CancellationToken cancellationToken)
    {
        var repo = _unitOfWork.Repository<ArtifactTracking>();
        var artifact = await (repo.GetByIdAsync(request.ArtifactId, cancellationToken)).ConfigureAwait(false);

        if (artifact == null || artifact.TenantId != request.TenantId || artifact.ProjectId != request.ProjectId || !artifact.IsActive)
        {
            return new ArtifactVerifyResult(
                IsValid: false,
                StoredHash: string.Empty,
                CurrentHash: string.Empty,
                ArtifactPath: string.Empty,
                FileExists: false);
        }

        var context = await (_artifactRepositoryService.GetProjectContextAsync(
            request.TenantId, request.ProjectId, cancellationToken)).ConfigureAwait(false);

        await _artifactRepositoryService.EnsureProjectSyncedAsync(context, cancellationToken)
            .ConfigureAwait(false);

        var fileInfo = await (_artifactRepositoryService.GetArtifactFileInfoAsync(
            context, artifact.ArtifactPath, cancellationToken)).ConfigureAwait(false);

        // A file that is gone is a different event from one whose contents moved, and it calls for
        // a different response. Folding it into "invalid" loses that.
        var currentHash = fileInfo.Exists ? fileInfo.Hash ?? string.Empty : string.Empty;
        var isValid = fileInfo.Exists
            && !string.IsNullOrWhiteSpace(currentHash)
            && !string.IsNullOrWhiteSpace(artifact.ContentHash)
            && string.Equals(currentHash, artifact.ContentHash, StringComparison.OrdinalIgnoreCase);

        request.AuditLog = new AuditLogRequest(
            ActorId: null,
            Action: "verify",
            ResourceType: "artifact",
            ResourceId: request.ArtifactId,
            // RequestedHash is recorded but was not used to decide anything. Keeping it separates a
            // caller sending a stale value from one sending none.
            Payload: new
            {
                artifact.ArtifactPath,
                IsValid = isValid,
                fileInfo.Exists,
                StoredHash = artifact.ContentHash,
                CurrentHash = currentHash,
                RequestedHash = request.CurrentHash,
            },
            TenantId: request.TenantId,
            ProjectId: request.ProjectId);

        return new ArtifactVerifyResult(
            IsValid: isValid,
            StoredHash: artifact.ContentHash,
            CurrentHash: currentHash,
            ArtifactPath: artifact.ArtifactPath,
            FileExists: fileInfo.Exists);
    }
}

/// <summary>
///
/// </summary>
public sealed record DeactivateArtifactCommand(
    GlobalUniqueId TenantId,
    GlobalUniqueId ProjectId,
    GlobalUniqueId ArtifactId) : ICommand<bool>, IAuditableRequest<bool>
{
    public AuditLogRequest? AuditLog { get; set; }
    public AuditLogRequest? GetAuditLogRequest(bool response) => response ? AuditLog : null;
}

public sealed class DeactivateArtifactCommandHandler : IRequestHandler<DeactivateArtifactCommand, bool>
{
    private readonly IUnitOfWork _unitOfWork;

    public DeactivateArtifactCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<bool> Handle(DeactivateArtifactCommand request, CancellationToken cancellationToken)
    {
        var repo = _unitOfWork.Repository<ArtifactTracking>();

        var artifact = await (repo.GetByIdAsync(request.ArtifactId, cancellationToken)).ConfigureAwait(false);
        if (artifact is null
            || artifact.TenantId != request.TenantId
            || artifact.ProjectId != request.ProjectId
            || !artifact.IsActive)
        {
            return false;
        }

        artifact.Deactivate();
        await (repo.UpdateAsync(artifact, cancellationToken)).ConfigureAwait(false);
        await (_unitOfWork.SaveChangesAsync(cancellationToken)).ConfigureAwait(false);

        request.AuditLog = new AuditLogRequest(
            ActorId: null,
            Action: AuditLog.Actions.Deleted,
            ResourceType: "artifact",
            ResourceId: artifact.Id,
            Payload: new { artifact.ArtifactPath, artifact.ArtifactType },
            TenantId: request.TenantId,
            ProjectId: request.ProjectId);

        return true;
    }
}

/// <summary>
/// ()
/// </summary>
public sealed record ActivateArtifactCommand(
    GlobalUniqueId TenantId,
    GlobalUniqueId ProjectId,
    GlobalUniqueId ArtifactId) : ICommand<bool>, IAuditableRequest<bool>
{
    public AuditLogRequest? AuditLog { get; set; }
    public AuditLogRequest? GetAuditLogRequest(bool response) => response ? AuditLog : null;
}

public sealed class ActivateArtifactCommandHandler : IRequestHandler<ActivateArtifactCommand, bool>
{
    private readonly IUnitOfWork _unitOfWork;

    public ActivateArtifactCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<bool> Handle(ActivateArtifactCommand request, CancellationToken cancellationToken)
    {
        var repo = _unitOfWork.Repository<ArtifactTracking>();

        var artifact = await (repo.GetByIdIncludingInactiveAsync(request.ArtifactId, cancellationToken)).ConfigureAwait(false);
        if (artifact is null
            || artifact.TenantId != request.TenantId
            || artifact.ProjectId != request.ProjectId)
        {
            return false;
        }

        if (artifact.IsActive)
        {
            throw new SddpException("ACTIVATE_ERROR", "Artifact is already active");
        }

        artifact.Activate();
        await (repo.UpdateAsync(artifact, cancellationToken)).ConfigureAwait(false);
        await (_unitOfWork.SaveChangesAsync(cancellationToken)).ConfigureAwait(false);

        request.AuditLog = new AuditLogRequest(
            ActorId: null,
            Action: "activate",
            ResourceType: "artifact",
            ResourceId: artifact.Id,
            Payload: new { artifact.ArtifactPath, artifact.ArtifactType },
            TenantId: request.TenantId,
            ProjectId: request.ProjectId);

        return true;
    }
}
