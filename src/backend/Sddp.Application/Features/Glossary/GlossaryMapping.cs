using Sddp.Abstractions.DTOs;
using Sddp.Abstractions.Interfaces;
using Sddp.Application.Utilities;
using Sddp.Domain.Entities;

namespace Sddp.Application.Features.Glossary;

internal static class GlossaryMapping
{
    internal static GlossaryTermDto MapToDto(GlossaryTerm term)
    {
        return new GlossaryTermDto(
            Id: term.Id.ToString(),
            TenantId: term.TenantId.ToString(),
            ProjectId: term.ProjectId.ToString(),
            Term: term.Term,
            Definition: term.Definition,
            Category: term.Category,
            Status: term.Status,
            Synonyms: term.Synonyms,
            Abbreviation: term.Abbreviation,
            Version: term.Version.ToString(),
            CreatedAt: term.CreatedAt.ToDateTimeOffset(),
            UpdatedAt: term.UpdatedAt.ToDateTimeOffset());
    }

    internal static GlossaryTermSummaryDto MapToSummaryDto(GlossaryTerm term)
    {
        var summaryDefinition = term.Definition.Length > 100
            ? term.Definition[..100] + "..."
            : term.Definition;

        return new GlossaryTermSummaryDto(
            Id: term.Id.ToString(),
            Term: term.Term,
            Definition: summaryDefinition,
            Category: term.Category,
            Status: term.Status);
    }

    internal static async Task<GlossaryTermDetailDto> MapToDetailDtoAsync(
        GlossaryTerm term,
        IUnitOfWork unitOfWork,
        CancellationToken cancellationToken)
    {
        var userRepo = unitOfWork.Repository<User>();
        var termRepo = unitOfWork.Repository<GlossaryTerm>();

        var definedBy = await (UserRefHelper.ToUserRefAsync(userRepo, term.DefinedBy, cancellationToken)).ConfigureAwait(false);
        var approvedBy = await (UserRefHelper.ToUserRefAsync(userRepo, term.ApprovedBy, cancellationToken)).ConfigureAwait(false);

        string? replacedByTermName = null;
        if (term.ReplacedByTermId.HasValue)
        {
            var replacedByTerm = await (termRepo.GetByIdAsync(term.ReplacedByTermId.Value, cancellationToken)).ConfigureAwait(false);
            replacedByTermName = replacedByTerm?.Term;
        }

        string? sourceSpecCode = null;
        string? sourceSpecTitle = null;
        if (term.SourceSpecId.HasValue)
        {
            var specRepo = unitOfWork.Repository<Spec>();
            var spec = await (specRepo.GetByIdAsync(term.SourceSpecId.Value, cancellationToken)).ConfigureAwait(false);
            sourceSpecCode = spec?.Code;
            sourceSpecTitle = spec?.Title;
        }

        string? sourceConversationName = null;
        string? sourceConversationType = null;
        if (term.SourceConversationId.HasValue)
        {
            var conversationRepo = unitOfWork.Repository<Conversation>();
            var conversation = await (conversationRepo.GetByIdAsync(term.SourceConversationId.Value, cancellationToken)).ConfigureAwait(false);
            sourceConversationName = conversation?.Name;
            sourceConversationType = conversation?.ConversationType.ToString();
        }

        string? sourceRequirementCode = null;
        string? sourceRequirementTitle = null;
        if (term.SourceRequirementId.HasValue)
        {
            var reqRepo = unitOfWork.Repository<Requirement>();
            var req = await (reqRepo.GetByIdAsync(term.SourceRequirementId.Value, cancellationToken)).ConfigureAwait(false);
            sourceRequirementCode = req?.Code;
            sourceRequirementTitle = req?.Title;
        }

        var owner = await (UserRefHelper.ToUserRefAsync(userRepo, term.OwnerUserId, cancellationToken)).ConfigureAwait(false);
        var createdBy = await (UserRefHelper.ToUserRefAsync(userRepo, term.CreatedBy, cancellationToken)).ConfigureAwait(false);
        var updatedBy = await (UserRefHelper.ToUserRefAsync(userRepo, term.UpdatedBy, cancellationToken)).ConfigureAwait(false);

        return new GlossaryTermDetailDto(
            Id: term.Id.ToString(),
            TenantId: term.TenantId.ToString(),
            ProjectId: term.ProjectId.ToString(),
            Term: term.Term,
            Definition: term.Definition,
            Category: term.Category,
            Status: term.Status,
            UsageExamples: term.UsageExamples,
            RelatedTermIds: term.RelatedTermIds.Select(id => id.ToString()).ToList(),
            Source: term.Source,
            Synonyms: term.Synonyms,
            Abbreviation: term.Abbreviation,
            DefinedBy: definedBy,
            ApprovedBy: approvedBy,
            ApprovedAt: term.ApprovedAt?.ToDateTimeOffset(),
            ReplacedByTermId: term.ReplacedByTermId?.ToString(),
            ReplacedByTermName: replacedByTermName,
            SourceSpecId: term.SourceSpecId?.ToString(),
            SourceSpecCode: sourceSpecCode,
            SourceSpecTitle: sourceSpecTitle,
            SourceConversationId: term.SourceConversationId?.ToString(),
            SourceConversationName: sourceConversationName,
            SourceConversationType: sourceConversationType,
            SourceRequirementId: term.SourceRequirementId?.ToString(),
            SourceRequirementCode: sourceRequirementCode,
            SourceRequirementTitle: sourceRequirementTitle,
            Owner: owner,
            Version: term.Version.ToString(),
            CreatedBy: createdBy,
            UpdatedBy: updatedBy,
            ValidFrom: term.ValidFrom.ToDateTimeOffset(),
            ValidTo: term.ValidTo?.ToDateTimeOffset(),
            CreatedAt: term.CreatedAt.ToDateTimeOffset(),
            UpdatedAt: term.UpdatedAt.ToDateTimeOffset());
    }

    internal static GlossaryTermVersionDto MapToVersionDto(
        GlossaryTerm term,
        UserRefDto? definedBy,
        UserRefDto? updatedBy)
    {
        return new GlossaryTermVersionDto(
            Id: term.Id.ToString(),
            Term: term.Term,
            Definition: term.Definition,
            Category: term.Category,
            Status: term.Status,
            Synonyms: term.Synonyms,
            Abbreviation: term.Abbreviation,
            Version: term.Version.ToString(),
            DefinedBy: definedBy,
            UpdatedBy: updatedBy,
            ValidFrom: term.ValidFrom.ToDateTimeOffset(),
            ValidTo: term.ValidTo?.ToDateTimeOffset(),
            CreatedAt: term.CreatedAt.ToDateTimeOffset(),
            UpdatedAt: term.UpdatedAt.ToDateTimeOffset());
    }
}
