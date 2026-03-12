using MediatR;
using Sddp.Abstractions.DTOs;
using Sddp.Abstractions.Enums;
using Sddp.Abstractions.Exceptions;
using Sddp.Abstractions.Interfaces;
using Sddp.Abstractions.ValueObjects;
using Sddp.Application.Features.Glossary;
using Sddp.Application.Requests;
using Sddp.Domain.Entities;

namespace Sddp.Application.Features.Glossary.Commands;

/// <summary>
/// glossary create
/// </summary>
public sealed record CreateGlossaryTermCommand(
    GlobalUniqueId TenantId,
    GlobalUniqueId ProjectId,
    GlobalUniqueId UserId,
    CreateGlossaryTermDto Dto) : ICommand<GlossaryTermDetailDto?>, IAuditableRequest<GlossaryTermDetailDto?>
{
    public AuditLogRequest? AuditLog { get; set; }
    public AuditLogRequest? GetAuditLogRequest(GlossaryTermDetailDto? response) => AuditLog;
}

public sealed class CreateGlossaryTermCommandHandler : IRequestHandler<CreateGlossaryTermCommand, GlossaryTermDetailDto?>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IEmbeddingTriggerService _embeddingTrigger;

    public CreateGlossaryTermCommandHandler(IUnitOfWork unitOfWork, IEmbeddingTriggerService embeddingTrigger)
    {
        _unitOfWork = unitOfWork;
        _embeddingTrigger = embeddingTrigger;
    }

    public async Task<GlossaryTermDetailDto?> Handle(CreateGlossaryTermCommand request, CancellationToken cancellationToken)
    {
        var termRepo = _unitOfWork.Repository<GlossaryTerm>();

        var existingCount = await (termRepo.CountAsync(
            t => t.TenantId == request.TenantId
                && t.ProjectId == request.ProjectId
                && t.Term == request.Dto.Term
                && t.IsActive
                && t.ValidTo == null,
            cancellationToken)).ConfigureAwait(false);

        if (existingCount > 0)
        {
            throw new SddpException("VALIDATION_ERROR", $"Term '{request.Dto.Term}' already exists");
        }

        GlobalUniqueId? sourceSpecId = null;
        if (!string.IsNullOrEmpty(request.Dto.SourceSpecId)
            && GlobalUniqueId.TryParse(request.Dto.SourceSpecId, out var parsedSpecId))
        {
            sourceSpecId = parsedSpecId;
        }

        GlobalUniqueId? sourceConversationId = null;
        if (!string.IsNullOrEmpty(request.Dto.SourceConversationId)
            && GlobalUniqueId.TryParse(request.Dto.SourceConversationId, out var parsedConvId))
        {
            sourceConversationId = parsedConvId;
        }

        GlobalUniqueId? sourceRequirementId = null;
        if (!string.IsNullOrEmpty(request.Dto.SourceRequirementId)
            && GlobalUniqueId.TryParse(request.Dto.SourceRequirementId, out var parsedReqId))
        {
            sourceRequirementId = parsedReqId;
        }

        GlobalUniqueId? ownerUserId = null;
        if (!string.IsNullOrEmpty(request.Dto.OwnerUserId)
            && GlobalUniqueId.TryParse(request.Dto.OwnerUserId, out var parsedOwnerId))
        {
            ownerUserId = parsedOwnerId;
        }

        var term = new GlossaryTerm(
            request.TenantId,
            request.ProjectId,
            request.Dto.Term,
            request.Dto.Definition,
            request.Dto.Category,
            request.UserId,
            request.Dto.Source,
            request.Dto.Synonyms,
            request.Dto.Abbreviation,
            sourceSpecId,
            sourceConversationId,
            sourceRequirementId,
            ownerUserId);

        term.SetCreatedBy(request.UserId);

        if (request.Dto.UsageExamples?.Count > 0)
        {
            term.SetUsageExamples(request.Dto.UsageExamples);
        }

        if (request.Dto.RelatedTermIds?.Count > 0)
        {
            var relatedIds = request.Dto.RelatedTermIds
                .Where(id => GlobalUniqueId.TryParse(id, out _))
                .Select(GlobalUniqueId.Parse)
                .ToList();
            term.SetRelatedTerms(relatedIds);
        }

        await (termRepo.AddAsync(term, cancellationToken)).ConfigureAwait(false);
        await (_unitOfWork.SaveChangesAsync(cancellationToken)).ConfigureAwait(false);

        _embeddingTrigger.TriggerGlossaryTermEmbedding(request.TenantId, request.ProjectId, term.Id);

        var changes = new object[]
        {
            new { field = "Term", oldValue = (string?)null, newValue = term.Term },
            new { field = "Definition", oldValue = (string?)null, newValue = term.Definition },
            new { field = "Category", oldValue = (string?)null, newValue = term.Category.ToString() },
            new { field = "Source", oldValue = (string?)null, newValue = term.Source },
            new { field = "Synonyms", oldValue = (string?)null, newValue = term.Synonyms },
            new { field = "Abbreviation", oldValue = (string?)null, newValue = term.Abbreviation },
        };

        request.AuditLog = new AuditLogRequest(
            request.UserId,
            "create",
            "glossary_term",
            term.Id,
            new { changes },
            request.TenantId,
            request.ProjectId);

        return await (GlossaryMapping.MapToDetailDtoAsync(term, _unitOfWork, cancellationToken)).ConfigureAwait(false);
    }
}

/// <summary>
/// glossary update (SCD Type 6 create)
/// </summary>
public sealed record UpdateGlossaryTermCommand(
    GlobalUniqueId TenantId,
    GlobalUniqueId ProjectId,
    GlobalUniqueId TermId,
    GlobalUniqueId UserId,
    UpdateGlossaryTermDto Dto) : ICommand<GlossaryTermDetailDto?>, IAuditableRequest<GlossaryTermDetailDto?>
{
    public AuditLogRequest? AuditLog { get; set; }
    public AuditLogRequest? GetAuditLogRequest(GlossaryTermDetailDto? response) => AuditLog;
}

public sealed class UpdateGlossaryTermCommandHandler : IRequestHandler<UpdateGlossaryTermCommand, GlossaryTermDetailDto?>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IEmbeddingTriggerService _embeddingTrigger;

    public UpdateGlossaryTermCommandHandler(IUnitOfWork unitOfWork, IEmbeddingTriggerService embeddingTrigger)
    {
        _unitOfWork = unitOfWork;
        _embeddingTrigger = embeddingTrigger;
    }

    public async Task<GlossaryTermDetailDto?> Handle(UpdateGlossaryTermCommand request, CancellationToken cancellationToken)
    {
        var termRepo = _unitOfWork.Repository<GlossaryTerm>();

        var existingTerm = await (termRepo.GetByIdAsync(request.TermId, cancellationToken)).ConfigureAwait(false);
        if (existingTerm is null
            || existingTerm.TenantId != request.TenantId
            || existingTerm.ProjectId != request.ProjectId
            || !existingTerm.IsActive)
        {
            return null;
        }

        // Capture old values for audit log (before modification)
        var oldDefinition = existingTerm.Definition;
        var oldCategory = existingTerm.Category.ToString();
        var oldSource = existingTerm.Source;
        var oldSynonyms = existingTerm.Synonyms;
        var oldAbbreviation = existingTerm.Abbreviation;

        // SCD Type 6: Create new version from current state
        var newTerm = existingTerm.CreateNextVersion();

        // Apply changes to new version
        var updateResult = newTerm.Update(
            request.Dto.Definition ?? existingTerm.Definition,
            request.Dto.Category ?? existingTerm.Category,
            request.Dto.Source ?? existingTerm.Source,
            request.Dto.Synonyms ?? existingTerm.Synonyms,
            request.Dto.Abbreviation ?? existingTerm.Abbreviation);
        updateResult.EnsureSuccess("VALIDATION_ERROR");

        newTerm.SetCreatedBy(request.UserId);
        newTerm.SetUpdatedBy(request.UserId);

        if (request.Dto.UsageExamples != null)
        {
            var examplesResult = newTerm.SetUsageExamples(request.Dto.UsageExamples);
            examplesResult.EnsureSuccess("VALIDATION_ERROR");
        }

        if (request.Dto.RelatedTermIds != null)
        {
            var relatedIds = request.Dto.RelatedTermIds
                .Where(id => GlobalUniqueId.TryParse(id, out _))
                .Select(GlobalUniqueId.Parse)
                .ToList();
            var relatedResult = newTerm.SetRelatedTerms(relatedIds);
            relatedResult.EnsureSuccess("VALIDATION_ERROR");
        }

        // Handle SourceSpecId link/unlink
        if (request.Dto.SourceSpecId != null)
        {
            if (string.IsNullOrEmpty(request.Dto.SourceSpecId))
            {
                newTerm.SetSourceSpec(null);
            }
            else if (GlobalUniqueId.TryParse(request.Dto.SourceSpecId, out var specId))
            {
                newTerm.SetSourceSpec(specId);
            }
        }

        // Handle SourceConversationId link/unlink
        if (request.Dto.SourceConversationId != null)
        {
            if (string.IsNullOrEmpty(request.Dto.SourceConversationId))
            {
                newTerm.SetSourceConversation(null);
            }
            else if (GlobalUniqueId.TryParse(request.Dto.SourceConversationId, out var convId))
            {
                newTerm.SetSourceConversation(convId);
            }
        }

        // Handle SourceRequirementId link/unlink
        if (request.Dto.SourceRequirementId != null)
        {
            if (string.IsNullOrEmpty(request.Dto.SourceRequirementId))
            {
                newTerm.SetSourceRequirement(null);
            }
            else if (GlobalUniqueId.TryParse(request.Dto.SourceRequirementId, out var reqId))
            {
                newTerm.SetSourceRequirement(reqId);
            }
        }

        // Handle OwnerUserId assignment/unassignment
        if (request.Dto.OwnerUserId != null)
        {
            if (string.IsNullOrEmpty(request.Dto.OwnerUserId))
            {
                newTerm.SetOwner(null);
            }
            else if (GlobalUniqueId.TryParse(request.Dto.OwnerUserId, out var ownerId))
            {
                newTerm.SetOwner(ownerId);
            }
        }

        // Expire old version and save new version
        existingTerm.Expire();
        await (termRepo.UpdateAsync(existingTerm, cancellationToken)).ConfigureAwait(false);
        await (termRepo.AddAsync(newTerm, cancellationToken)).ConfigureAwait(false);
        await (_unitOfWork.SaveChangesAsync(cancellationToken)).ConfigureAwait(false);

        _embeddingTrigger.TriggerGlossaryTermEmbedding(request.TenantId, request.ProjectId, newTerm.Id);

        // Build audit log with field-level changes
        var changes = new List<object>();
        var newDefinition = request.Dto.Definition ?? existingTerm.Definition;
        var newCategory = (request.Dto.Category ?? existingTerm.Category).ToString();
        var newSource = request.Dto.Source ?? existingTerm.Source;
        var newSynonyms = request.Dto.Synonyms ?? existingTerm.Synonyms;
        var newAbbreviation = request.Dto.Abbreviation ?? existingTerm.Abbreviation;

        if (oldDefinition != newDefinition)
            changes.Add(new { field = "Definition", oldValue = oldDefinition, newValue = newDefinition });
        if (oldCategory != newCategory)
            changes.Add(new { field = "Category", oldValue = oldCategory, newValue = newCategory });
        if (oldSource != newSource)
            changes.Add(new { field = "Source", oldValue = oldSource, newValue = newSource });
        if (oldSynonyms != newSynonyms)
            changes.Add(new { field = "Synonyms", oldValue = oldSynonyms, newValue = newSynonyms });
        if (oldAbbreviation != newAbbreviation)
            changes.Add(new { field = "Abbreviation", oldValue = oldAbbreviation, newValue = newAbbreviation });

        request.AuditLog = new AuditLogRequest(
            request.UserId,
            "update",
            "glossary_term",
            newTerm.Id,
            new { changes },
            request.TenantId,
            request.ProjectId);

        return await (GlossaryMapping.MapToDetailDtoAsync(newTerm, _unitOfWork, cancellationToken)).ConfigureAwait(false);
    }
}

/// <summary>
/// glossary approve
/// </summary>
public sealed record ApproveGlossaryTermCommand(
    GlobalUniqueId TenantId,
    GlobalUniqueId ProjectId,
    GlobalUniqueId TermId,
    GlobalUniqueId UserId) : ICommand<GlossaryTermDto?>, IAuditableRequest<GlossaryTermDto?>
{
    public AuditLogRequest? AuditLog { get; set; }
    public AuditLogRequest? GetAuditLogRequest(GlossaryTermDto? response) => AuditLog;
}

public sealed class ApproveGlossaryTermCommandHandler : IRequestHandler<ApproveGlossaryTermCommand, GlossaryTermDto?>
{
    private readonly IUnitOfWork _unitOfWork;

    public ApproveGlossaryTermCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<GlossaryTermDto?> Handle(ApproveGlossaryTermCommand request, CancellationToken cancellationToken)
    {
        var termRepo = _unitOfWork.Repository<GlossaryTerm>();

        var term = await (termRepo.GetByIdAsync(request.TermId, cancellationToken)).ConfigureAwait(false);
        if (term is null
            || term.TenantId != request.TenantId
            || term.ProjectId != request.ProjectId
            || !term.IsActive)
        {
            return null;
        }

        var approveResult = term.Approve(request.UserId);
        approveResult.EnsureSuccess("APPROVAL_ERROR");

        term.SetUpdatedBy(request.UserId);

        await (termRepo.UpdateAsync(term, cancellationToken)).ConfigureAwait(false);
        await (_unitOfWork.SaveChangesAsync(cancellationToken)).ConfigureAwait(false);

        request.AuditLog = new AuditLogRequest(
            request.UserId,
            "approve",
            "glossary_term",
            request.TermId,
            null,
            request.TenantId,
            request.ProjectId);

        return GlossaryMapping.MapToDto(term);
    }
}

/// <summary>
/// glossary (glossary)
/// </summary>
public sealed record DeprecateGlossaryTermCommand(
    GlobalUniqueId TenantId,
    GlobalUniqueId ProjectId,
    GlobalUniqueId TermId,
    GlobalUniqueId UserId,
    GlobalUniqueId? ReplacementTermId) : ICommand<GlossaryTermDto?>, IAuditableRequest<GlossaryTermDto?>
{
    public AuditLogRequest? AuditLog { get; set; }
    public AuditLogRequest? GetAuditLogRequest(GlossaryTermDto? response) => AuditLog;
}

public sealed class DeprecateGlossaryTermCommandHandler : IRequestHandler<DeprecateGlossaryTermCommand, GlossaryTermDto?>
{
    private readonly IUnitOfWork _unitOfWork;

    public DeprecateGlossaryTermCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<GlossaryTermDto?> Handle(DeprecateGlossaryTermCommand request, CancellationToken cancellationToken)
    {
        var termRepo = _unitOfWork.Repository<GlossaryTerm>();

        var term = await (termRepo.GetByIdAsync(request.TermId, cancellationToken)).ConfigureAwait(false);
        if (term is null
            || term.TenantId != request.TenantId
            || term.ProjectId != request.ProjectId
            || !term.IsActive)
        {
            return null;
        }

        if (request.ReplacementTermId.HasValue)
        {
            var replacement = await (termRepo.GetByIdAsync(request.ReplacementTermId.Value, cancellationToken)).ConfigureAwait(false);
            if (replacement is null || !replacement.IsActive || replacement.Status != GlossaryTermStatus.Active)
            {
                throw new SddpException("DEPRECATION_ERROR", "Replacement term not found or not active");
            }
        }

        var deprecateResult = term.Deprecate(request.ReplacementTermId);
        deprecateResult.EnsureSuccess("DEPRECATION_ERROR");

        term.SetUpdatedBy(request.UserId);

        await (termRepo.UpdateAsync(term, cancellationToken)).ConfigureAwait(false);
        await (_unitOfWork.SaveChangesAsync(cancellationToken)).ConfigureAwait(false);

        request.AuditLog = new AuditLogRequest(
            request.UserId,
            "deprecate",
            "glossary_term",
            request.TermId,
            new { ReplacementTermId = request.ReplacementTermId?.ToString() },
            request.TenantId,
            request.ProjectId);

        return GlossaryMapping.MapToDto(term);
    }
}

/// <summary>
/// glossary
/// </summary>
public sealed record ReactivateGlossaryTermCommand(
    GlobalUniqueId TenantId,
    GlobalUniqueId ProjectId,
    GlobalUniqueId TermId,
    GlobalUniqueId UserId) : ICommand<GlossaryTermDto?>, IAuditableRequest<GlossaryTermDto?>
{
    public AuditLogRequest? AuditLog { get; set; }
    public AuditLogRequest? GetAuditLogRequest(GlossaryTermDto? response) => AuditLog;
}

public sealed class ReactivateGlossaryTermCommandHandler : IRequestHandler<ReactivateGlossaryTermCommand, GlossaryTermDto?>
{
    private readonly IUnitOfWork _unitOfWork;

    public ReactivateGlossaryTermCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<GlossaryTermDto?> Handle(ReactivateGlossaryTermCommand request, CancellationToken cancellationToken)
    {
        var termRepo = _unitOfWork.Repository<GlossaryTerm>();

        var term = await (termRepo.GetByIdAsync(request.TermId, cancellationToken)).ConfigureAwait(false);
        if (term is null
            || term.TenantId != request.TenantId
            || term.ProjectId != request.ProjectId
            || !term.IsActive)
        {
            return null;
        }

        var reactivateResult = term.Reactivate(request.UserId);
        reactivateResult.EnsureSuccess("REACTIVATION_ERROR");

        term.SetUpdatedBy(request.UserId);

        await (termRepo.UpdateAsync(term, cancellationToken)).ConfigureAwait(false);
        await (_unitOfWork.SaveChangesAsync(cancellationToken)).ConfigureAwait(false);

        request.AuditLog = new AuditLogRequest(
            request.UserId,
            "reactivate",
            "glossary_term",
            request.TermId,
            null,
            request.TenantId,
            request.ProjectId);

        return GlossaryMapping.MapToDto(term);
    }
}

/// <summary>
/// glossary delete ()
/// </summary>
public sealed record DeleteGlossaryTermCommand(
    GlobalUniqueId TenantId,
    GlobalUniqueId ProjectId,
    GlobalUniqueId TermId) : ICommand<bool>, IAuditableRequest<bool>
{
    public AuditLogRequest? AuditLog { get; set; }
    public AuditLogRequest? GetAuditLogRequest(bool response) => response ? AuditLog : null;
}

public sealed class DeleteGlossaryTermCommandHandler : IRequestHandler<DeleteGlossaryTermCommand, bool>
{
    private readonly IUnitOfWork _unitOfWork;

    public DeleteGlossaryTermCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<bool> Handle(DeleteGlossaryTermCommand request, CancellationToken cancellationToken)
    {
        var termRepo = _unitOfWork.Repository<GlossaryTerm>();

        var term = await (termRepo.GetByIdAsync(request.TermId, cancellationToken)).ConfigureAwait(false);
        if (term is null
            || term.TenantId != request.TenantId
            || term.ProjectId != request.ProjectId
            || !term.IsActive)
        {
            return false;
        }

        term.Deactivate();
        await (termRepo.UpdateAsync(term, cancellationToken)).ConfigureAwait(false);
        await (_unitOfWork.SaveChangesAsync(cancellationToken)).ConfigureAwait(false);

        request.AuditLog = new AuditLogRequest(
            null,
            "delete",
            "glossary_term",
            request.TermId,
            null,
            request.TenantId,
            request.ProjectId);

        return true;
    }
}

/// <summary>
/// glossary glossary
/// </summary>
public sealed record AddGlossaryRelatedTermCommand(
    GlobalUniqueId TenantId,
    GlobalUniqueId ProjectId,
    GlobalUniqueId TermId,
    GlobalUniqueId RelatedTermId) : ICommand<GlossaryTermDto?>, IAuditableRequest<GlossaryTermDto?>
{
    public AuditLogRequest? AuditLog { get; set; }
    public AuditLogRequest? GetAuditLogRequest(GlossaryTermDto? response) => AuditLog;
}

public sealed class AddGlossaryRelatedTermCommandHandler : IRequestHandler<AddGlossaryRelatedTermCommand, GlossaryTermDto?>
{
    private readonly IUnitOfWork _unitOfWork;

    public AddGlossaryRelatedTermCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<GlossaryTermDto?> Handle(AddGlossaryRelatedTermCommand request, CancellationToken cancellationToken)
    {
        var termRepo = _unitOfWork.Repository<GlossaryTerm>();

        var term = await (termRepo.GetByIdAsync(request.TermId, cancellationToken)).ConfigureAwait(false);
        if (term is null
            || term.TenantId != request.TenantId
            || term.ProjectId != request.ProjectId
            || !term.IsActive)
        {
            return null;
        }

        var relatedTerm = await (termRepo.GetByIdAsync(request.RelatedTermId, cancellationToken)).ConfigureAwait(false);
        if (relatedTerm is null || !relatedTerm.IsActive)
        {
            throw new SddpException("ADD_RELATED_ERROR", "Related term not found");
        }

        var addRelatedResult = term.AddRelatedTerm(request.RelatedTermId);
        addRelatedResult.EnsureSuccess("ADD_RELATED_ERROR");
        await (termRepo.UpdateAsync(term, cancellationToken)).ConfigureAwait(false);
        await (_unitOfWork.SaveChangesAsync(cancellationToken)).ConfigureAwait(false);

        request.AuditLog = new AuditLogRequest(
            null,
            "add_related",
            "glossary_term",
            request.TermId,
            new { RelatedTermId = request.RelatedTermId.ToString() },
            request.TenantId,
            request.ProjectId);

        return GlossaryMapping.MapToDto(term);
    }
}

/// <summary>
/// glossary
/// </summary>
public sealed record AddGlossaryUsageExampleCommand(
    GlobalUniqueId TenantId,
    GlobalUniqueId ProjectId,
    GlobalUniqueId TermId,
    string Example) : ICommand<GlossaryTermDto?>, IAuditableRequest<GlossaryTermDto?>
{
    public AuditLogRequest? AuditLog { get; set; }
    public AuditLogRequest? GetAuditLogRequest(GlossaryTermDto? response) => AuditLog;
}

public sealed class AddGlossaryUsageExampleCommandHandler : IRequestHandler<AddGlossaryUsageExampleCommand, GlossaryTermDto?>
{
    private readonly IUnitOfWork _unitOfWork;

    public AddGlossaryUsageExampleCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<GlossaryTermDto?> Handle(AddGlossaryUsageExampleCommand request, CancellationToken cancellationToken)
    {
        var termRepo = _unitOfWork.Repository<GlossaryTerm>();

        var term = await (termRepo.GetByIdAsync(request.TermId, cancellationToken)).ConfigureAwait(false);
        if (term is null
            || term.TenantId != request.TenantId
            || term.ProjectId != request.ProjectId
            || !term.IsActive)
        {
            return null;
        }

        var addExampleResult = term.AddUsageExample(request.Example);
        addExampleResult.EnsureSuccess("ADD_EXAMPLE_ERROR");

        await (termRepo.UpdateAsync(term, cancellationToken)).ConfigureAwait(false);
        await (_unitOfWork.SaveChangesAsync(cancellationToken)).ConfigureAwait(false);

        request.AuditLog = new AuditLogRequest(
            null,
            "add_usage",
            "glossary_term",
            request.TermId,
            new { request.Example },
            request.TenantId,
            request.ProjectId);

        return GlossaryMapping.MapToDto(term);
    }
}
