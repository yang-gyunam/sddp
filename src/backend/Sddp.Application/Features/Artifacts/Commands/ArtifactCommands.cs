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
    GlobalUniqueId? ArtifactId = null) : ICommand<ArtifactTrackingDto>, IAuditableRequest<ArtifactTrackingDto>
{
    public AuditLogRequest? AuditLog { get; set; }
    public AuditLogRequest? GetAuditLogRequest(ArtifactTrackingDto response) => AuditLog;
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

/// <summary>
/// create
/// </summary>
public sealed record RegenerateArtifactCommand(
    GlobalUniqueId TenantId,
    GlobalUniqueId ProjectId,
    GlobalUniqueId ArtifactId) : ICommand<ArtifactRegenerateResult>, IAuditableRequest<ArtifactRegenerateResult>
{
    public AuditLogRequest? AuditLog { get; set; }
    public AuditLogRequest? GetAuditLogRequest(ArtifactRegenerateResult response) => AuditLog;
}

public sealed class RegenerateArtifactCommandHandler : IRequestHandler<RegenerateArtifactCommand, ArtifactRegenerateResult>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IGenerationService? _generationService;

    public RegenerateArtifactCommandHandler(IUnitOfWork unitOfWork, IGenerationService? generationService = null)
    {
        _unitOfWork = unitOfWork;
        _generationService = generationService;
    }

    public async Task<ArtifactRegenerateResult> Handle(RegenerateArtifactCommand request, CancellationToken cancellationToken)
    {
        var repo = _unitOfWork.Repository<ArtifactTracking>();
        var artifact = await (repo.GetByIdAsync(request.ArtifactId, cancellationToken)).ConfigureAwait(false);

        if (artifact == null || artifact.TenantId != request.TenantId || artifact.ProjectId != request.ProjectId || !artifact.IsActive)
        {
            throw new SddpException("REGENERATE_ERROR", "Artifact not found");
        }

        if (_generationService is null)
        {
            throw new SddpException("REGENERATE_ERROR", "Generation service is not available");
        }

        var previousHash = artifact.ContentHash;

        var generationResult = await (_generationService.GenerateFromSpecAsync(
            artifact.SpecId,
            new GenerationOptions(),
            cancellationToken)).ConfigureAwait(false);

        if (!generationResult.Success)
        {
            throw new SddpException(
                "REGENERATE_ERROR", $"Regeneration failed: {string.Join(", ", generationResult.Errors)}");
        }

        var regenerated = generationResult.Artifacts
            .FirstOrDefault(a => a.FilePath == artifact.ArtifactPath);

        if (regenerated is null)
        {
            throw new SddpException(
                "REGENERATE_ERROR", $"Regenerated artifact not found for path: {artifact.ArtifactPath}");
        }

        var newHash = ArtifactStatusHelper.ComputeHash(regenerated.Content);
        artifact.UpdateHash(newHash, artifact.GeneratorVersion, artifact.TemplateVersion);
        await (repo.UpdateAsync(artifact, cancellationToken)).ConfigureAwait(false);
        await (_unitOfWork.SaveChangesAsync(cancellationToken)).ConfigureAwait(false);

        request.AuditLog = new AuditLogRequest(
            ActorId: null,
            Action: "regenerate",
            ResourceType: "artifact",
            ResourceId: request.ArtifactId,
            Payload: new { artifact.ArtifactPath, artifact.ArtifactType, PreviousHash = previousHash, NewHash = newHash },
            TenantId: request.TenantId,
            ProjectId: request.ProjectId);

        return new ArtifactRegenerateResult(
            ArtifactId: artifact.Id.ToString(),
            SpecId: artifact.SpecId.ToString(),
            ArtifactPath: artifact.ArtifactPath,
            NewContentHash: newHash,
            PreviousContentHash: previousHash,
            RegeneratedAt: DateTimeOffset.UtcNow);
    }
}

/// <summary>
///
/// </summary>
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

    public VerifyArtifactCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
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
                CurrentHash: request.CurrentHash,
                ArtifactPath: string.Empty);
        }

        var isValid = artifact.VerifyHash(request.CurrentHash);

        request.AuditLog = new AuditLogRequest(
            ActorId: null,
            Action: "verify",
            ResourceType: "artifact",
            ResourceId: request.ArtifactId,
            Payload: new { artifact.ArtifactPath, IsValid = isValid, StoredHash = artifact.ContentHash, request.CurrentHash },
            TenantId: request.TenantId,
            ProjectId: request.ProjectId);

        return new ArtifactVerifyResult(
            IsValid: isValid,
            StoredHash: artifact.ContentHash,
            CurrentHash: request.CurrentHash,
            ArtifactPath: artifact.ArtifactPath);
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
