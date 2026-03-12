using MediatR;
using Microsoft.Extensions.Logging;
using Sddp.Abstractions.Constants;
using Sddp.Abstractions.DTOs;
using Sddp.Abstractions.Enums;
using Sddp.Abstractions.Exceptions;
using Sddp.Abstractions.Interfaces;
using Sddp.Abstractions.ValueObjects;
using Sddp.Application.Features.Conversations.Commands;
using Sddp.Application.Requests;
using Sddp.Domain.Entities;
using AuditActions = Sddp.Domain.Entities.AuditLog.Actions;

namespace Sddp.Application.Features.Requirements.Commands;

/// <summary>
/// requirement create
/// </summary>
public sealed record CreateRequirementCommand(
    GlobalUniqueId TenantId,
    GlobalUniqueId ProjectId,
    GlobalUniqueId UserId,
    CreateRequirementDto Dto) : ICommand<RequirementDetailDto?>, IAuditableRequest<RequirementDetailDto?>
{
    public AuditLogRequest? AuditLog { get; set; }

    public AuditLogRequest? GetAuditLogRequest(RequirementDetailDto? response) => AuditLog;
}

public sealed class CreateRequirementCommandHandler : IRequestHandler<CreateRequirementCommand, RequirementDetailDto?>
{
    private readonly IUnitOfWork _unitOfWork;

    public CreateRequirementCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<RequirementDetailDto?> Handle(CreateRequirementCommand request, CancellationToken cancellationToken)
    {
        var requirementRepo = _unitOfWork.Repository<Requirement>();

        var existingCount = await (requirementRepo.CountAsync(
            r => r.TenantId == request.TenantId
                && r.ProjectId == request.ProjectId
                && r.Code == request.Dto.Code
                && r.IsActive,
            cancellationToken)).ConfigureAwait(false);

        if (existingCount > 0)
        {
            throw new SddpException("VALIDATION_ERROR", $"Requirement with code '{request.Dto.Code}' already exists");
        }

        GlobalUniqueId? parentId = null;
        RequirementLevel autoLevel;

        if (!string.IsNullOrEmpty(request.Dto.ParentId))
        {
            if (!GlobalUniqueId.TryParse(request.Dto.ParentId, out var parsedParentId))
            {
                throw new SddpException("VALIDATION_ERROR", "Invalid parent ID format");
            }

            var parent = await (requirementRepo.GetByIdAsync(parsedParentId, cancellationToken)).ConfigureAwait(false);
            if (parent is null || !parent.IsActive)
            {
                throw new SddpException("VALIDATION_ERROR", "Parent requirement not found");
            }

            autoLevel = parent.Level switch
            {
                RequirementLevel.A => RequirementLevel.B,
                RequirementLevel.B => RequirementLevel.C,
                _ => throw new SddpException("VALIDATION_ERROR",
                    $"Cannot create child under Level {parent.Level} (max depth is C)")
            };
            parentId = parsedParentId;
        }
        else
        {
            autoLevel = RequirementLevel.A;
        }

        GlobalUniqueId? ownerUserId = null;
        if (!string.IsNullOrEmpty(request.Dto.OwnerUserId))
        {
            if (!GlobalUniqueId.TryParse(request.Dto.OwnerUserId, out var parsedOwnerId))
                throw new SddpException("VALIDATION_ERROR", "Invalid owner user ID format");
            ownerUserId = parsedOwnerId;
        }

        var requirement = new Requirement(
            request.TenantId,
            request.ProjectId,
            request.Dto.Code,
            request.Dto.Title,
            request.Dto.Description,
            autoLevel,
            request.Dto.Priority,
            parentId,
            ownerUserId);

        requirement.SetCreatedBy(request.UserId);

        await (requirementRepo.AddAsync(requirement, cancellationToken)).ConfigureAwait(false);
        await (_unitOfWork.SaveChangesAsync(cancellationToken)).ConfigureAwait(false);

        request.AuditLog = new AuditLogRequest(
            request.UserId,
            "create",
            "requirement",
            requirement.Id,
            new { requirement.Code, requirement.Title, Level = requirement.Level.ToString() },
            request.TenantId,
            request.ProjectId);

        return await (RequirementMapping.MapToDetailDtoAsync(requirement, _unitOfWork, cancellationToken)).ConfigureAwait(false);
    }
}

/// <summary>
/// requirement update
/// </summary>
public sealed record UpdateRequirementCommand(
    GlobalUniqueId TenantId,
    GlobalUniqueId ProjectId,
    GlobalUniqueId RequirementId,
    GlobalUniqueId UserId,
    UpdateRequirementDto Dto) : ICommand<RequirementDetailDto?>, IAuditableRequest<RequirementDetailDto?>
{
    public AuditLogRequest? AuditLog { get; set; }

    public AuditLogRequest? GetAuditLogRequest(RequirementDetailDto? response) => AuditLog;
}

public sealed class UpdateRequirementCommandHandler : IRequestHandler<UpdateRequirementCommand, RequirementDetailDto?>
{
    private readonly IUnitOfWork _unitOfWork;

    public UpdateRequirementCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<RequirementDetailDto?> Handle(UpdateRequirementCommand request, CancellationToken cancellationToken)
    {
        var requirementRepo = _unitOfWork.Repository<Requirement>();

        var requirement = await (requirementRepo.GetByIdAsync(request.RequirementId, cancellationToken)).ConfigureAwait(false);
        if (requirement is null
            || requirement.TenantId != request.TenantId
            || requirement.ProjectId != request.ProjectId
            || !requirement.IsActive)
        {
            return null;
        }

        // SCD Type 6: Create historical snapshot before modifying
        var snapshot = requirement.CreateHistoricalSnapshot();
        await (requirementRepo.AddAsync(snapshot, cancellationToken)).ConfigureAwait(false);

        // Capture before-snapshot for change tracking (FieldAuthorBadge)
        var beforeSnapshot = RequirementMapping.CaptureRequirementSnapshot(requirement);

        var priority = request.Dto.Priority ?? requirement.Priority;
        var updateResult = requirement.Update(request.Dto.Title, request.Dto.Description, priority);
        updateResult.EnsureSuccess("VALIDATION_ERROR");

        if (request.Dto.ParentId is not null)
        {
            if (string.IsNullOrEmpty(request.Dto.ParentId))
            {
                var setParentResult = requirement.SetParent(null);
                setParentResult.EnsureSuccess("VALIDATION_ERROR");
            }
            else
            {
                if (!GlobalUniqueId.TryParse(request.Dto.ParentId, out var parsedParentId))
                    throw new SddpException("VALIDATION_ERROR", "Invalid parent ID format");

                if (parsedParentId == requirement.Id)
                    throw new SddpException("VALIDATION_ERROR", "Requirement cannot be its own parent");

                var parent = await (requirementRepo.GetByIdAsync(parsedParentId, cancellationToken)).ConfigureAwait(false);
                if (parent is null || !parent.IsActive || parent.ValidTo != null)
                    throw new SddpException("VALIDATION_ERROR", "Parent requirement not found");

                RequirementCommandValidation.ValidateParentChildLevel(parent.Level, requirement.Level);

                var setParentResult2 = requirement.SetParent(parsedParentId);
                setParentResult2.EnsureSuccess("VALIDATION_ERROR");
            }
        }

        if (request.Dto.OwnerUserId is not null)
        {
            if (string.IsNullOrEmpty(request.Dto.OwnerUserId))
                requirement.SetOwner(null);
            else if (GlobalUniqueId.TryParse(request.Dto.OwnerUserId, out var parsedOwnerId))
                requirement.SetOwner(parsedOwnerId);
            else
                throw new SddpException("VALIDATION_ERROR", "Invalid owner user ID format");
        }

        requirement.SetUpdatedBy(request.UserId);
        requirement.ResetValidFrom();

        await (requirementRepo.UpdateAsync(requirement, cancellationToken)).ConfigureAwait(false);
        await (_unitOfWork.SaveChangesAsync(cancellationToken)).ConfigureAwait(false);

        var changes = RequirementMapping.BuildRequirementChanges(beforeSnapshot, requirement);
        request.AuditLog = new AuditLogRequest(
            request.UserId,
            "update",
            "requirement",
            requirement.Id,
            new { changes },
            request.TenantId,
            request.ProjectId);

        return await (RequirementMapping.MapToDetailDtoAsync(requirement, _unitOfWork, cancellationToken)).ConfigureAwait(false);
    }
}

/// <summary>
/// requirement status
/// </summary>
public sealed record TransitionRequirementStatusCommand(
    GlobalUniqueId TenantId,
    GlobalUniqueId ProjectId,
    GlobalUniqueId RequirementId,
    GlobalUniqueId UserId,
    RequirementStatus NewStatus) : ICommand<RequirementDto?>, IAuditableRequest<RequirementDto?>
{
    public AuditLogRequest? AuditLog { get; set; }

    public AuditLogRequest? GetAuditLogRequest(RequirementDto? response) => AuditLog;
}

public sealed class TransitionRequirementStatusCommandHandler : IRequestHandler<TransitionRequirementStatusCommand, RequirementDto?>
{
    private readonly IUnitOfWork _unitOfWork;

    public TransitionRequirementStatusCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<RequirementDto?> Handle(TransitionRequirementStatusCommand request, CancellationToken cancellationToken)
    {
        var requirementRepo = _unitOfWork.Repository<Requirement>();

        var requirement = await (requirementRepo.GetByIdAsync(request.RequirementId, cancellationToken)).ConfigureAwait(false);
        if (requirement is null
            || requirement.TenantId != request.TenantId
            || requirement.ProjectId != request.ProjectId
            || !requirement.IsActive)
        {
            return null;
        }

        // SCD Type 6: Create historical snapshot before modifying
        var snapshot = requirement.CreateHistoricalSnapshot();
        await (requirementRepo.AddAsync(snapshot, cancellationToken)).ConfigureAwait(false);

        var fromStatus = requirement.Status;
        var transitionResult = requirement.TransitionTo(request.NewStatus);
        transitionResult.EnsureSuccess("TRANSITION_ERROR");
        requirement.SetUpdatedBy(request.UserId);
        requirement.ResetValidFrom();

        await (requirementRepo.UpdateAsync(requirement, cancellationToken)).ConfigureAwait(false);
        await (_unitOfWork.SaveChangesAsync(cancellationToken)).ConfigureAwait(false);

        request.AuditLog = new AuditLogRequest(
            request.UserId,
            AuditActions.StatusChanged,
            "requirement",
            requirement.Id,
            new { requirement.Code, FromStatus = fromStatus.ToString(), ToStatus = request.NewStatus.ToString() },
            request.TenantId,
            request.ProjectId);

        var childrenCount = await (requirementRepo.CountAsync(
            c => c.ParentId == requirement.Id && c.IsActive && c.ValidTo == null,
            cancellationToken)).ConfigureAwait(false);

        return RequirementMapping.MapToDto(requirement, childrenCount);
    }
}

/// <summary>
/// requirement delete
/// </summary>
public sealed record DeleteRequirementCommand(
    GlobalUniqueId TenantId,
    GlobalUniqueId ProjectId,
    GlobalUniqueId RequirementId) : ICommand<bool>, IAuditableRequest<bool>
{
    public AuditLogRequest? AuditLog { get; set; }

    public AuditLogRequest? GetAuditLogRequest(bool response) => response ? AuditLog : null;
}

public sealed class DeleteRequirementCommandHandler : IRequestHandler<DeleteRequirementCommand, bool>
{
    private readonly IUnitOfWork _unitOfWork;

    public DeleteRequirementCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<bool> Handle(DeleteRequirementCommand request, CancellationToken cancellationToken)
    {
        var requirementRepo = _unitOfWork.Repository<Requirement>();

        var requirement = await (requirementRepo.GetByIdAsync(request.RequirementId, cancellationToken)).ConfigureAwait(false);
        if (requirement is null
            || requirement.TenantId != request.TenantId
            || requirement.ProjectId != request.ProjectId
            || !requirement.IsActive)
        {
            return false;
        }

        var childrenCount = await (requirementRepo.CountAsync(
            c => c.ParentId == requirement.Id && c.IsActive,
            cancellationToken)).ConfigureAwait(false);

        if (childrenCount > 0)
        {
            throw new SddpException("DELETE_ERROR", "Cannot delete requirement with children");
        }

        requirement.Deactivate();
        await (requirementRepo.UpdateAsync(requirement, cancellationToken)).ConfigureAwait(false);
        await (_unitOfWork.SaveChangesAsync(cancellationToken)).ConfigureAwait(false);

        request.AuditLog = new AuditLogRequest(
            null,
            AuditActions.Deleted,
            "requirement",
            requirement.Id,
            new { requirement.Code, requirement.Title, Level = requirement.Level.ToString() },
            request.TenantId,
            request.ProjectId);

        return true;
    }
}

/// <summary>
/// requirement-conversation
/// </summary>
public sealed record LinkRequirementConversationCommand(
    GlobalUniqueId TenantId,
    GlobalUniqueId ProjectId,
    GlobalUniqueId RequirementId,
    GlobalUniqueId ConversationId,
    GlobalUniqueId UserId) : ICommand<RequirementDto?>, IAuditableRequest<RequirementDto?>
{
    public AuditLogRequest? AuditLog { get; set; }
    public AuditLogRequest? GetAuditLogRequest(RequirementDto? response) => AuditLog;
}

public sealed class LinkRequirementConversationCommandHandler : IRequestHandler<LinkRequirementConversationCommand, RequirementDto?>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ISender _sender;
    private readonly IConversationHubService _hubService;
    private readonly ILogger<LinkRequirementConversationCommandHandler> _logger;

    public LinkRequirementConversationCommandHandler(
        IUnitOfWork unitOfWork,
        ISender sender,
        IConversationHubService hubService,
        ILogger<LinkRequirementConversationCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _sender = sender;
        _hubService = hubService;
        _logger = logger;
    }

    public async Task<RequirementDto?> Handle(LinkRequirementConversationCommand request, CancellationToken cancellationToken)
    {
        var requirementRepo = _unitOfWork.Repository<Requirement>();
        var conversationRepo = _unitOfWork.Repository<Conversation>();

        var requirement = await (requirementRepo.GetByIdAsync(request.RequirementId, cancellationToken)).ConfigureAwait(false);
        if (requirement is null
            || requirement.TenantId != request.TenantId
            || requirement.ProjectId != request.ProjectId
            || !requirement.IsActive)
        {
            return null;
        }

        var conversation = await (conversationRepo.GetByIdAsync(request.ConversationId, cancellationToken)).ConfigureAwait(false);
        if (conversation is null
            || conversation.TenantId != request.TenantId
            || !conversation.IsActive)
        {
            throw new SddpException("LINK_ERROR", "Conversation not found");
        }

        var previousConversationId = requirement.ConversationId?.ToString();
        requirement.LinkConversation(request.ConversationId);
        requirement.SetUpdatedBy(request.UserId);
        await (requirementRepo.UpdateAsync(requirement, cancellationToken)).ConfigureAwait(false);
        await (_unitOfWork.SaveChangesAsync(cancellationToken)).ConfigureAwait(false);

        // Post AI reminder message to the conversation + broadcast via SignalR
        try
        {
            var aiUserId = GlobalUniqueId.FromGuid(WellKnownUsers.AiAgentUserId);
            var content = $"Requirement [{requirement.Code}] \"{requirement.Title}\" has been linked to this conversation.";
            var messageResult = await (_sender.Send(new PostConversationMessageCommand(
                request.TenantId,
                conversation.ProjectId,
                request.ConversationId,
                aiUserId,
                MessageType.AiReminder,
                content,
                null,
                null), cancellationToken)).ConfigureAwait(false);

            if (messageResult is not null)
            {
                var msg = messageResult.Message;
                await (_hubService.BroadcastNewMessageAsync(
                    request.ConversationId.ToString(),
                    new MessageDto(
                        msg.Id, msg.ConversationId, msg.Sender,
                        msg.Type,
                        msg.Content, msg.References ?? Array.Empty<string>(),
                        msg.ReplyToId, msg.IsEdited,
                        msg.CreatedAt, msg.UpdatedAt))).ConfigureAwait(false);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to post AI reminder for requirement link {RequirementId} → {ConversationId}",
                request.RequirementId, request.ConversationId);
        }

        var changes = new List<object>
        {
            new { field = "ConversationId", oldValue = previousConversationId, newValue = request.ConversationId.ToString() }
        };
        request.AuditLog = new AuditLogRequest(
            request.UserId,
            "update",
            "requirement",
            request.RequirementId,
            new { changes },
            request.TenantId,
            request.ProjectId);

        var childrenCount = await (requirementRepo.CountAsync(
            c => c.ParentId == requirement.Id && c.IsActive && c.ValidTo == null,
            cancellationToken)).ConfigureAwait(false);

        return RequirementMapping.MapToDto(requirement, childrenCount);
    }
}

/// <summary>
/// requirement-conversation
/// </summary>
public sealed record UnlinkRequirementConversationCommand(
    GlobalUniqueId TenantId,
    GlobalUniqueId ProjectId,
    GlobalUniqueId RequirementId,
    GlobalUniqueId UserId) : ICommand<RequirementDto?>, IAuditableRequest<RequirementDto?>
{
    public AuditLogRequest? AuditLog { get; set; }
    public AuditLogRequest? GetAuditLogRequest(RequirementDto? response) => AuditLog;
}

public sealed class UnlinkRequirementConversationCommandHandler : IRequestHandler<UnlinkRequirementConversationCommand, RequirementDto?>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ISender _sender;
    private readonly IConversationHubService _hubService;
    private readonly ILogger<UnlinkRequirementConversationCommandHandler> _logger;

    public UnlinkRequirementConversationCommandHandler(
        IUnitOfWork unitOfWork,
        ISender sender,
        IConversationHubService hubService,
        ILogger<UnlinkRequirementConversationCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _sender = sender;
        _hubService = hubService;
        _logger = logger;
    }

    public async Task<RequirementDto?> Handle(UnlinkRequirementConversationCommand request, CancellationToken cancellationToken)
    {
        var requirementRepo = _unitOfWork.Repository<Requirement>();
        var conversationRepo = _unitOfWork.Repository<Conversation>();

        var requirement = await (requirementRepo.GetByIdAsync(request.RequirementId, cancellationToken)).ConfigureAwait(false);
        if (requirement is null
            || requirement.TenantId != request.TenantId
            || requirement.ProjectId != request.ProjectId
            || !requirement.IsActive)
        {
            return null;
        }

        var previousConversationId = requirement.ConversationId;
        requirement.UnlinkConversation();
        requirement.SetUpdatedBy(request.UserId);
        await (requirementRepo.UpdateAsync(requirement, cancellationToken)).ConfigureAwait(false);
        await (_unitOfWork.SaveChangesAsync(cancellationToken)).ConfigureAwait(false);

        // Post AI reminder message to the previously linked conversation + broadcast via SignalR
        if (previousConversationId.HasValue)
        {
            try
            {
                var conversation = await (conversationRepo.GetByIdAsync(previousConversationId.Value, cancellationToken)).ConfigureAwait(false);
                var aiUserId = GlobalUniqueId.FromGuid(WellKnownUsers.AiAgentUserId);
                var content = $"Requirement [{requirement.Code}] \"{requirement.Title}\" has been unlinked from this conversation.";
                var messageResult = await (_sender.Send(new PostConversationMessageCommand(
                    request.TenantId,
                    conversation?.ProjectId,
                    previousConversationId.Value,
                    aiUserId,
                    MessageType.AiReminder,
                    content,
                    null,
                    null), cancellationToken)).ConfigureAwait(false);

                if (messageResult is not null)
                {
                    var msg = messageResult.Message;
                    await (_hubService.BroadcastNewMessageAsync(
                        previousConversationId.Value.ToString(),
                        new MessageDto(
                            msg.Id, msg.ConversationId, msg.Sender,
                            msg.Type,
                            msg.Content, msg.References ?? Array.Empty<string>(),
                            msg.ReplyToId, msg.IsEdited,
                            msg.CreatedAt, msg.UpdatedAt))).ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to post AI reminder for requirement unlink {RequirementId} from {ConversationId}",
                    request.RequirementId, previousConversationId.Value);
            }
        }

        var changes = new List<object>
        {
            new { field = "ConversationId", oldValue = previousConversationId?.ToString(), newValue = (string?)null }
        };
        request.AuditLog = new AuditLogRequest(
            request.UserId,
            "update",
            "requirement",
            request.RequirementId,
            new { changes },
            request.TenantId,
            request.ProjectId);

        var childrenCount = await (requirementRepo.CountAsync(
            c => c.ParentId == requirement.Id && c.IsActive && c.ValidTo == null,
            cancellationToken)).ConfigureAwait(false);

        return RequirementMapping.MapToDto(requirement, childrenCount);
    }
}

internal static class RequirementCommandValidation
{
    internal static void ValidateParentChildLevel(RequirementLevel parentLevel, RequirementLevel childLevel)
    {
        var isValid = (parentLevel, childLevel) switch
        {
            (RequirementLevel.A, RequirementLevel.B) => true,
            (RequirementLevel.B, RequirementLevel.C) => true,
            _ => false
        };

        if (!isValid)
        {
            throw new SddpException("VALIDATION_ERROR",
                $"Invalid level hierarchy: Level {childLevel} cannot be a child of Level {parentLevel}");
        }
    }
}
