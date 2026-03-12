using MediatR;
using Sddp.Abstractions.DTOs;
using Sddp.Abstractions.Enums;
using Sddp.Abstractions.Exceptions;
using Sddp.Abstractions.Interfaces;
using Sddp.Abstractions.ValueObjects;
using Sddp.Application.Requests;
using Sddp.Domain.Entities;

namespace Sddp.Application.Features.Specs.Commands;

/// <summary>
/// spec create
/// </summary>
public sealed record CreateSpecCommand(
    GlobalUniqueId TenantId,
    GlobalUniqueId ProjectId,
    GlobalUniqueId UserId,
    CreateSpecDto Dto) : ICommand<SpecDetailDto?>, IAuditableRequest<SpecDetailDto?>
{
    public AuditLogRequest? AuditLog { get; set; }

    public AuditLogRequest? GetAuditLogRequest(SpecDetailDto? response) => AuditLog;
}

public sealed class CreateSpecCommandHandler : IRequestHandler<CreateSpecCommand, SpecDetailDto?>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IEmbeddingTriggerService _embeddingTrigger;
    private readonly IAnalysisTriggerService _analysisTrigger;

    public CreateSpecCommandHandler(
        IUnitOfWork unitOfWork,
        IEmbeddingTriggerService embeddingTrigger,
        IAnalysisTriggerService analysisTrigger)
    {
        _unitOfWork = unitOfWork;
        _embeddingTrigger = embeddingTrigger;
        _analysisTrigger = analysisTrigger;
    }

    public async Task<SpecDetailDto?> Handle(CreateSpecCommand request, CancellationToken cancellationToken)
    {
        var specRepo = _unitOfWork.Repository<Spec>();

        var existingCount = await (specRepo.CountAsync(
            s => s.TenantId == request.TenantId
                && s.ProjectId == request.ProjectId
                && s.Code == request.Dto.Code
                && s.IsActive,
            cancellationToken)).ConfigureAwait(false);

        if (existingCount > 0)
        {
            throw new SddpException("VALIDATION_ERROR", $"Spec with code '{request.Dto.Code}' already exists");
        }

        GlobalUniqueId? requirementId = null;
        if (!string.IsNullOrEmpty(request.Dto.RequirementId))
        {
            if (!GlobalUniqueId.TryParse(request.Dto.RequirementId, out var parsedRequirementId))
            {
                throw new SddpException("VALIDATION_ERROR", "Invalid requirement ID format");
            }

            var requirementRepo = _unitOfWork.Repository<Requirement>();
            var requirement = await (requirementRepo.GetByIdAsync(parsedRequirementId, cancellationToken)).ConfigureAwait(false);
            if (requirement is null || !requirement.IsActive)
            {
                throw new SddpException("VALIDATION_ERROR", "Requirement not found");
            }

            requirementId = parsedRequirementId;
        }

        GlobalUniqueId? bornFromConversationId = null;
        if (!string.IsNullOrEmpty(request.Dto.BornFromConversationId))
        {
            if (!GlobalUniqueId.TryParse(request.Dto.BornFromConversationId, out var parsedConversationId))
            {
                throw new SddpException("VALIDATION_ERROR", "Invalid Conversation ID format");
            }

            var conversationRepo = _unitOfWork.Repository<Conversation>();
            var conversation = await (conversationRepo.GetByIdAsync(parsedConversationId, cancellationToken)).ConfigureAwait(false);
            if (conversation is null || !conversation.IsActive)
            {
                throw new SddpException("VALIDATION_ERROR", "Conversation not found");
            }

            bornFromConversationId = parsedConversationId;
        }

        var spec = new Spec(
            request.TenantId,
            request.ProjectId,
            request.Dto.Code,
            request.Dto.Title,
            request.Dto.Description,
            request.Dto.Decision,
            request.Dto.Context,
            request.Dto.Scope,
            request.Dto.OutOfScope,
            request.Dto.Definitions,
            request.Dto.AcceptanceCriteria,
            request.Dto.Owners,
            request.Dto.ReviewTrigger,
            requirementId,
            bornFromConversationId);

        spec.SetCreatedBy(request.UserId);

        await (specRepo.AddAsync(spec, cancellationToken)).ConfigureAwait(false);
        await (_unitOfWork.SaveChangesAsync(cancellationToken)).ConfigureAwait(false);

        request.AuditLog = new AuditLogRequest(
            request.UserId,
            "create",
            "spec",
            spec.Id,
            new
            {
                spec.Code,
                spec.Title,
                Status = spec.Status.ToString()
            },
            request.TenantId,
            request.ProjectId);

        _embeddingTrigger.TriggerSpecEmbedding(request.TenantId, request.ProjectId, spec.Id);

        await (_analysisTrigger.TriggerAsync(
            request.TenantId, request.ProjectId, "Conflict",
            spec.Id, "spec", cancellationToken: cancellationToken)).ConfigureAwait(false);

        return await (SpecMapping.MapToDetailDtoAsync(spec, _unitOfWork, cancellationToken)).ConfigureAwait(false);
    }
}

/// <summary>
/// spec update
/// </summary>
public sealed record UpdateSpecCommand(
    GlobalUniqueId TenantId,
    GlobalUniqueId ProjectId,
    GlobalUniqueId SpecId,
    GlobalUniqueId UserId,
    UpdateSpecDto Dto) : ICommand<SpecDetailDto?>, IAuditableRequest<SpecDetailDto?>
{
    public AuditLogRequest? AuditLog { get; set; }

    public AuditLogRequest? GetAuditLogRequest(SpecDetailDto? response) => AuditLog;
}

public sealed class UpdateSpecCommandHandler : IRequestHandler<UpdateSpecCommand, SpecDetailDto?>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IEmbeddingTriggerService _embeddingTrigger;
    private readonly IAnalysisTriggerService _analysisTrigger;

    public UpdateSpecCommandHandler(
        IUnitOfWork unitOfWork,
        IEmbeddingTriggerService embeddingTrigger,
        IAnalysisTriggerService analysisTrigger)
    {
        _unitOfWork = unitOfWork;
        _embeddingTrigger = embeddingTrigger;
        _analysisTrigger = analysisTrigger;
    }

    public async Task<SpecDetailDto?> Handle(UpdateSpecCommand request, CancellationToken cancellationToken)
    {
        var specRepo = _unitOfWork.Repository<Spec>();

        var spec = await (specRepo.GetByIdAsync(request.SpecId, cancellationToken)).ConfigureAwait(false);
        if (spec is null
            || spec.TenantId != request.TenantId
            || spec.ProjectId != request.ProjectId
            || !spec.IsActive)
        {
            return null;
        }

        if (spec.Status == SpecStatus.Locked)
        {
            throw new SpecLockedException(spec.Id);
        }

        var beforeSnapshot = SpecMapping.CaptureSpecSnapshot(spec);
        var updateResult = spec.Update(
            request.Dto.Title,
            request.Dto.Description,
            request.Dto.Decision,
            request.Dto.Context,
            request.Dto.Scope,
            request.Dto.OutOfScope,
            request.Dto.Definitions,
            request.Dto.AcceptanceCriteria,
            request.Dto.Owners,
            request.Dto.ReviewTrigger);
        updateResult.EnsureSuccess("VALIDATION_ERROR");

        // Handle RequirementId link/unlink
        if (!string.IsNullOrEmpty(request.Dto.RequirementId))
        {
            if (GlobalUniqueId.TryParse(request.Dto.RequirementId, out var reqId)
                && spec.RequirementId != reqId)
            {
                var linkResult = spec.LinkRequirement(reqId);
                linkResult.EnsureSuccess("VALIDATION_ERROR");
            }
        }
        else if (spec.RequirementId.HasValue)
        {
            var unlinkResult = spec.UnlinkRequirement();
            unlinkResult.EnsureSuccess("VALIDATION_ERROR");
        }

        // Handle BornFromConversationId link/unlink
        if (!string.IsNullOrEmpty(request.Dto.BornFromConversationId))
        {
            if (GlobalUniqueId.TryParse(request.Dto.BornFromConversationId, out var convId)
                && spec.BornFromConversationId != convId)
            {
                spec.LinkConversation(convId);
            }
        }
        else if (spec.BornFromConversationId.HasValue)
        {
            spec.UnlinkConversation();
        }

        spec.SetUpdatedBy(request.UserId);

        await (specRepo.UpdateAsync(spec, cancellationToken)).ConfigureAwait(false);
        await (_unitOfWork.SaveChangesAsync(cancellationToken)).ConfigureAwait(false);

        var changes = SpecMapping.BuildSpecChanges(beforeSnapshot, spec);
        if (changes.Count > 0)
        {
            request.AuditLog = new AuditLogRequest(
                request.UserId,
                "update",
                "spec",
                spec.Id,
                new { changes },
                request.TenantId,
                request.ProjectId);
        }

        _embeddingTrigger.TriggerSpecEmbedding(request.TenantId, request.ProjectId, request.SpecId);

        await (_analysisTrigger.TriggerAsync(
            request.TenantId, request.ProjectId, "Conflict",
            request.SpecId, "spec", cancellationToken: cancellationToken)).ConfigureAwait(false);
        await (_analysisTrigger.TriggerAsync(
            request.TenantId, request.ProjectId, "Impact",
            request.SpecId, "spec", cancellationToken: cancellationToken)).ConfigureAwait(false);

        return await (SpecMapping.MapToDetailDtoAsync(spec, _unitOfWork, cancellationToken)).ConfigureAwait(false);
    }
}

/// <summary>
/// spec delete
/// </summary>
public sealed record DeleteSpecCommand(
    GlobalUniqueId TenantId,
    GlobalUniqueId ProjectId,
    GlobalUniqueId SpecId) : ICommand<bool>, IAuditableRequest<bool>
{
    public AuditLogRequest? AuditLog { get; set; }

    public AuditLogRequest? GetAuditLogRequest(bool response) => response ? AuditLog : null;
}

public sealed class DeleteSpecCommandHandler : IRequestHandler<DeleteSpecCommand, bool>
{
    private readonly IUnitOfWork _unitOfWork;

    public DeleteSpecCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<bool> Handle(DeleteSpecCommand request, CancellationToken cancellationToken)
    {
        var specRepo = _unitOfWork.Repository<Spec>();

        var spec = await (specRepo.GetByIdAsync(request.SpecId, cancellationToken)).ConfigureAwait(false);
        if (spec is null
            || spec.TenantId != request.TenantId
            || spec.ProjectId != request.ProjectId
            || !spec.IsActive)
        {
            return false;
        }

        if (spec.Status == SpecStatus.Locked)
        {
            throw new SddpException("DELETE_ERROR", "Cannot delete locked spec");
        }

        spec.Deactivate();
        await (specRepo.UpdateAsync(spec, cancellationToken)).ConfigureAwait(false);
        await (_unitOfWork.SaveChangesAsync(cancellationToken)).ConfigureAwait(false);

        request.AuditLog = new AuditLogRequest(
            null,
            AuditLog.Actions.Deleted,
            "spec",
            spec.Id,
            new { spec.Code, spec.Title, Status = spec.Status.ToString() },
            request.TenantId,
            request.ProjectId);

        return true;
    }
}

/// <summary>
/// spec ()
/// </summary>
public sealed record ActivateSpecCommand(
    GlobalUniqueId TenantId,
    GlobalUniqueId ProjectId,
    GlobalUniqueId SpecId) : ICommand<bool>, IAuditableRequest<bool>
{
    public AuditLogRequest? AuditLog { get; set; }

    public AuditLogRequest? GetAuditLogRequest(bool response) => response ? AuditLog : null;
}

public sealed class ActivateSpecCommandHandler : IRequestHandler<ActivateSpecCommand, bool>
{
    private readonly IUnitOfWork _unitOfWork;

    public ActivateSpecCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<bool> Handle(ActivateSpecCommand request, CancellationToken cancellationToken)
    {
        var specRepo = _unitOfWork.Repository<Spec>();

        var spec = await (specRepo.GetByIdIncludingInactiveAsync(request.SpecId, cancellationToken)).ConfigureAwait(false);
        if (spec is null
            || spec.TenantId != request.TenantId
            || spec.ProjectId != request.ProjectId)
        {
            return false;
        }

        if (spec.IsActive)
        {
            throw new SddpException("ACTIVATE_ERROR", "Spec is already active");
        }

        spec.Activate();
        await (specRepo.UpdateAsync(spec, cancellationToken)).ConfigureAwait(false);
        await (_unitOfWork.SaveChangesAsync(cancellationToken)).ConfigureAwait(false);

        request.AuditLog = new AuditLogRequest(
            null,
            "activate",
            "spec",
            spec.Id,
            new { spec.Code, spec.Title, Status = spec.Status.ToString() },
            request.TenantId,
            request.ProjectId);

        return true;
    }
}
