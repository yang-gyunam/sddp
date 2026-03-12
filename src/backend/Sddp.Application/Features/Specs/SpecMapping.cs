using Sddp.Abstractions.DTOs;
using Sddp.Abstractions.Enums;
using Sddp.Abstractions.Interfaces;
using Sddp.Abstractions.ValueObjects;
using Sddp.Application.Utilities;
using Sddp.Domain.Entities;

namespace Sddp.Application.Features.Specs;

internal record SpecMappingContext(
    IReadOnlyDictionary<string, Requirement> Requirements,
    IReadOnlyDictionary<string, Conversation> Conversations,
    IReadOnlyDictionary<string, UserRefDto> Users)
{
    internal static async Task<SpecMappingContext> BuildAsync(
        IUnitOfWork unitOfWork,
        IEnumerable<Spec> specs,
        CancellationToken cancellationToken)
    {
        var specList = specs.ToList();

        var reqIds = specList.Where(s => s.RequirementId.HasValue)
            .Select(s => s.RequirementId!.Value).Distinct().ToList();
        var reqRepo = unitOfWork.Repository<Requirement>();
        var reqs = reqIds.Count > 0
            ? await (reqRepo.FindAsync(r => reqIds.Contains(r.Id), cancellationToken))
.ConfigureAwait(false)            : [];
        var reqMap = reqs.ToDictionary(r => r.Id.ToString());

        var convIds = specList.Where(s => s.BornFromConversationId.HasValue)
            .Select(s => s.BornFromConversationId!.Value).Distinct().ToList();
        var convRepo = unitOfWork.Repository<Conversation>();
        var convs = convIds.Count > 0
            ? await (convRepo.FindAsync(c => convIds.Contains(c.Id), cancellationToken))
.ConfigureAwait(false)            : [];
        var convMap = convs.ToDictionary(c => c.Id.ToString());

        var userIds = specList
            .SelectMany(s => new[] { s.CreatedBy, s.UpdatedBy })
            .Where(id => id != default).Distinct().ToList();
        var userRepo = unitOfWork.Repository<User>();
        var userMap = await (UserRefHelper.ResolveMapAsync(userRepo, userIds, cancellationToken)).ConfigureAwait(false);

        return new SpecMappingContext(reqMap, convMap, userMap);
    }
}

internal static class SpecMapping
{
    internal static SpecDto MapToDto(Spec spec)
    {
        return new SpecDto(
            Id: spec.Id.ToString(),
            TenantId: spec.TenantId.ToString(),
            ProjectId: spec.ProjectId.ToString(),
            Code: spec.Code,
            Title: spec.Title,
            Description: spec.Description ?? string.Empty,
            Decision: spec.Decision ?? string.Empty,
            Status: spec.Status,
            RequirementId: spec.RequirementId?.ToString(),
            BornFromConversationId: spec.BornFromConversationId?.ToString(),
            SupersedesSpecId: spec.SupersedesSpecId?.ToString(),
            Version: spec.Version.ToString(),
            LockedAt: spec.LockedAt?.ToDateTimeOffset(),
            CreatedAt: spec.CreatedAt.ToDateTimeOffset(),
            UpdatedAt: spec.UpdatedAt.ToDateTimeOffset());
    }

    internal static async Task<SpecDetailDto> MapToDetailDtoAsync(
        Spec spec,
        IUnitOfWork unitOfWork,
        CancellationToken cancellationToken)
    {
        var ctx = await (SpecMappingContext.BuildAsync(unitOfWork, [spec], cancellationToken)).ConfigureAwait(false);
        return MapToDetailDto(spec, ctx);
    }

    internal static SpecDetailDto MapToDetailDto(Spec spec, SpecMappingContext ctx)
    {
        string? requirementCode = null;
        string? requirementTitle = null;
        if (spec.RequirementId.HasValue &&
            ctx.Requirements.TryGetValue(spec.RequirementId.Value.ToString(), out var requirement))
        {
            requirementCode = requirement.Code;
            requirementTitle = requirement.Title;
        }

        string? bornFromConversationName = null;
        string? bornFromConversationType = null;
        string? bornFromConversationDescription = null;
        if (spec.BornFromConversationId.HasValue &&
            ctx.Conversations.TryGetValue(spec.BornFromConversationId.Value.ToString(), out var conversation))
        {
            bornFromConversationName = conversation.Name;
            bornFromConversationType = conversation.ConversationType.ToString();
            bornFromConversationDescription = conversation.Description;
        }

        ctx.Users.TryGetValue(spec.CreatedBy.ToString(), out var createdBy);
        ctx.Users.TryGetValue(spec.UpdatedBy.ToString(), out var updatedBy);

        return new SpecDetailDto(
            Id: spec.Id.ToString(),
            TenantId: spec.TenantId.ToString(),
            ProjectId: spec.ProjectId.ToString(),
            Code: spec.Code,
            Title: spec.Title,
            Description: spec.Description,
            Decision: spec.Decision,
            Context: spec.Context,
            Scope: spec.Scope,
            OutOfScope: spec.OutOfScope,
            Definitions: spec.Definitions,
            AcceptanceCriteria: spec.AcceptanceCriteria,
            Owners: spec.Owners.ToNullableCsv(),
            ReviewTrigger: spec.ReviewTrigger,
            Status: spec.Status,
            RequirementId: spec.RequirementId?.ToString(),
            RequirementCode: requirementCode,
            RequirementTitle: requirementTitle,
            BornFromConversationId: spec.BornFromConversationId?.ToString(),
            BornFromConversationName: bornFromConversationName,
            BornFromConversationType: bornFromConversationType,
            BornFromConversationDescription: bornFromConversationDescription,
            SupersedesSpecId: spec.SupersedesSpecId?.ToString(),
            Version: spec.Version.ToString(),
            CreatedBy: createdBy ?? new UserRefDto(Id: spec.CreatedBy.ToString(), Name: null, AvatarUrl: null),
            UpdatedBy: updatedBy ?? new UserRefDto(Id: spec.UpdatedBy.ToString(), Name: null, AvatarUrl: null),
            LockedAt: spec.LockedAt?.ToDateTimeOffset(),
            ValidFrom: spec.ValidFrom.ToDateTimeOffset(),
            ValidTo: spec.ValidTo?.ToDateTimeOffset(),
            CreatedAt: spec.CreatedAt.ToDateTimeOffset(),
            UpdatedAt: spec.UpdatedAt.ToDateTimeOffset());
    }

    internal static SignOffDto MapToSignOffDto(SignOff signOff, UserRefDto stakeholder)
    {
        return new SignOffDto(
            Id: signOff.Id.ToString(),
            SpecId: signOff.SpecId.ToString(),
            Stakeholder: stakeholder,
            Role: signOff.Role,
            Decision: signOff.Decision,
            Conditions: signOff.Conditions,
            Comments: signOff.Comments,
            SignedAt: signOff.SignedAt?.ToDateTimeOffset(),
            CreatedAt: signOff.CreatedAt.ToDateTimeOffset(),
            UpdatedAt: signOff.UpdatedAt.ToDateTimeOffset());
    }

    internal static SpecSummaryDto MapToSummaryDto(Spec spec)
    {
        return new SpecSummaryDto(
            Id: spec.Id.ToString(),
            Code: spec.Code,
            Title: spec.Title,
            Status: spec.Status.ToString());
    }

    internal static Dictionary<string, string?> CaptureSpecSnapshot(Spec spec)
    {
        return new Dictionary<string, string?>
        {
            ["Title"] = spec.Title,
            ["Description"] = spec.Description,
            ["Decision"] = spec.Decision,
            ["Context"] = spec.Context,
            ["Scope"] = spec.Scope,
            ["OutOfScope"] = spec.OutOfScope,
            ["Definitions"] = spec.Definitions,
            ["AcceptanceCriteria"] = spec.AcceptanceCriteria,
            ["Owners"] = spec.Owners.ToNullableCsv(),
            ["ReviewTrigger"] = spec.ReviewTrigger,
            ["RequirementId"] = spec.RequirementId?.ToString(),
            ["BornFromConversationId"] = spec.BornFromConversationId?.ToString()
        };
    }

    internal static List<object> BuildSpecChanges(
        Dictionary<string, string?> before,
        Spec spec)
    {
        var after = CaptureSpecSnapshot(spec);
        var changes = new List<object>();

        foreach (var entry in before)
        {
            var beforeValue = entry.Value ?? string.Empty;
            var afterValue = after[entry.Key] ?? string.Empty;
            if (!string.Equals(beforeValue, afterValue, StringComparison.Ordinal))
            {
                changes.Add(new
                {
                    field = entry.Key,
                    oldValue = entry.Value,
                    newValue = after[entry.Key]
                });
            }
        }

        return changes;
    }
}
