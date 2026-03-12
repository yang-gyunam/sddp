using Sddp.Abstractions.DTOs;
using Sddp.Abstractions.Enums;
using Sddp.Abstractions.Interfaces;
using Sddp.Abstractions.ValueObjects;
using Sddp.Application.Utilities;
using Sddp.Domain.Entities;

namespace Sddp.Application.Features.Requirements;

internal static class RequirementMapping
{
    internal static RequirementSummaryDto MapToSummaryDto(Requirement requirement)
    {
        return new RequirementSummaryDto(
            Id: requirement.Id.ToString(),
            Code: requirement.Code,
            Title: requirement.Title,
            Level: requirement.Level);
    }

    internal static RequirementDto MapToDto(Requirement requirement, int childrenCount)
    {
        return new RequirementDto(
            Id: requirement.Id.ToString(),
            TenantId: requirement.TenantId.ToString(),
            ProjectId: requirement.ProjectId.ToString(),
            Code: requirement.Code,
            Title: requirement.Title,
            Description: requirement.Description,
            Level: requirement.Level,
            Priority: requirement.Priority,
            Status: requirement.Status,
            ParentId: requirement.ParentId?.ToString(),
            ConversationId: requirement.ConversationId?.ToString(),
            Version: requirement.Version.ToString(),
            ChildrenCount: childrenCount,
            CreatedAt: requirement.CreatedAt.ToDateTimeOffset(),
            UpdatedAt: requirement.UpdatedAt.ToDateTimeOffset());
    }

    internal static async Task<RequirementDetailDto> MapToDetailDtoAsync(
        Requirement requirement,
        IUnitOfWork unitOfWork,
        CancellationToken cancellationToken)
    {
        var requirementRepo = unitOfWork.Repository<Requirement>();

        // Build ancestor chain (root -> immediate parent order)
        var ancestors = new List<RequirementAncestorDto>();
        string? parentCode = null;
        string? parentTitle = null;
        RequirementLevel? parentLevel = null;
        if (requirement.ParentId.HasValue)
        {
            var currentParentId = requirement.ParentId;
            while (currentParentId.HasValue)
            {
                var ancestor = await (requirementRepo.GetByIdAsync(currentParentId.Value, cancellationToken)).ConfigureAwait(false);
                if (ancestor == null) break;
                ancestors.Insert(0, new RequirementAncestorDto(
                    ancestor.Id.ToString(), ancestor.Code, ancestor.Title, ancestor.Level));
                currentParentId = ancestor.ParentId;
            }

            if (ancestors.Count > 0)
            {
                var immediateParent = ancestors[^1];
                parentCode = immediateParent.Code;
                parentTitle = immediateParent.Title;
                parentLevel = immediateParent.Level;
            }
        }

        // Look up linked conversation info
        string? conversationName = null;
        string? conversationDescription = null;
        ConversationType? conversationType = null;
        if (requirement.ConversationId.HasValue)
        {
            var conversationRepo = unitOfWork.Repository<Conversation>();
            var conversation = await (conversationRepo.GetByIdAsync(requirement.ConversationId.Value, cancellationToken)).ConfigureAwait(false);
            if (conversation != null)
            {
                conversationName = conversation.Name;
                conversationDescription = conversation.Description;
                conversationType = conversation.ConversationType;
            }
        }

        // Fetch siblings (same-level peers, excluding current)
        var siblings = new List<RequirementAncestorDto>();
        {
            IEnumerable<Requirement> siblingEntities;
            if (requirement.ParentId.HasValue)
            {
                siblingEntities = await (requirementRepo.FindAsync(
                    s => s.ParentId == requirement.ParentId && s.Id != requirement.Id && s.IsActive && s.ValidTo == null,
                    cancellationToken)).ConfigureAwait(false);
            }
            else if (requirement.ConversationId.HasValue)
            {
                siblingEntities = await (requirementRepo.FindAsync(
                    s => s.ConversationId == requirement.ConversationId && s.ParentId == null
                         && s.Id != requirement.Id && s.IsActive && s.ValidTo == null,
                    cancellationToken)).ConfigureAwait(false);
            }
            else
            {
                siblingEntities = [];
            }
            siblings = siblingEntities
                .OrderBy(s => s.Code)
                .Select(s => new RequirementAncestorDto(s.Id.ToString(), s.Code, s.Title, s.Level))
                .ToList();
        }

        var userRepo = unitOfWork.Repository<User>();
        var owner = await (UserRefHelper.ToUserRefAsync(userRepo, requirement.OwnerUserId, cancellationToken)).ConfigureAwait(false);
        var createdBy = await (UserRefHelper.ToUserRefAsync(userRepo, requirement.CreatedBy, cancellationToken)).ConfigureAwait(false);
        var updatedBy = await (UserRefHelper.ToUserRefAsync(userRepo, requirement.UpdatedBy, cancellationToken)).ConfigureAwait(false);

        var children = await (requirementRepo.FindAsync(
            c => c.ParentId == requirement.Id && c.IsActive && c.ValidTo == null,
            cancellationToken)).ConfigureAwait(false);

        var childDtos = new List<RequirementDto>();
        foreach (var child in children.OrderBy(c => c.Code))
        {
            var grandChildrenCount = await (requirementRepo.CountAsync(
                gc => gc.ParentId == child.Id && gc.IsActive && gc.ValidTo == null,
                cancellationToken)).ConfigureAwait(false);

            childDtos.Add(MapToDto(child, grandChildrenCount));
        }

        // Reverse lookup: Specs that reference this Requirement
        var specRepo = unitOfWork.Repository<Spec>();
        var linkedSpecEntities = await (specRepo.FindAsync(
            s => s.RequirementId == requirement.Id && s.IsActive && s.ValidTo == null,
            cancellationToken)).ConfigureAwait(false);
        var linkedSpecs = linkedSpecEntities
            .OrderBy(s => s.Code)
            .Select(s => new LinkedSpecDto(s.Id.ToString(), s.Code, s.Title, s.Status))
            .ToList();

        return new RequirementDetailDto(
            Id: requirement.Id.ToString(),
            TenantId: requirement.TenantId.ToString(),
            ProjectId: requirement.ProjectId.ToString(),
            Code: requirement.Code,
            Title: requirement.Title,
            Description: requirement.Description,
            Level: requirement.Level,
            Priority: requirement.Priority,
            Status: requirement.Status,
            ParentId: requirement.ParentId?.ToString(),
            ParentCode: parentCode,
            ParentTitle: parentTitle,
            ParentLevel: parentLevel,
            ConversationId: requirement.ConversationId?.ToString(),
            ConversationName: conversationName,
            ConversationDescription: conversationDescription,
            ConversationType: conversationType,
            Version: requirement.Version.ToString(),
            Ancestors: ancestors,
            Siblings: siblings,
            Children: childDtos,
            LinkedSpecs: linkedSpecs,
            Owner: owner,
            CreatedBy: createdBy,
            UpdatedBy: updatedBy,
            ValidFrom: requirement.ValidFrom.ToDateTimeOffset(),
            ValidTo: requirement.ValidTo?.ToDateTimeOffset(),
            CreatedAt: requirement.CreatedAt.ToDateTimeOffset(),
            UpdatedAt: requirement.UpdatedAt.ToDateTimeOffset());
    }

    internal static RequirementVersionDto MapToVersionDto(
        Requirement requirement,
        UserRefDto? owner = null,
        UserRefDto? createdBy = null,
        UserRefDto? updatedBy = null)
    {
        return new RequirementVersionDto(
            Id: requirement.Id.ToString(),
            Code: requirement.Code,
            Title: requirement.Title,
            Description: requirement.Description,
            Level: requirement.Level,
            Priority: requirement.Priority,
            Status: requirement.Status,
            ParentId: requirement.ParentId?.ToString(),
            ConversationId: requirement.ConversationId?.ToString(),
            Version: requirement.Version.ToString(),
            Owner: owner,
            CreatedBy: createdBy,
            UpdatedBy: updatedBy,
            ValidFrom: requirement.ValidFrom.ToDateTimeOffset(),
            ValidTo: requirement.ValidTo?.ToDateTimeOffset(),
            CreatedAt: requirement.CreatedAt.ToDateTimeOffset(),
            UpdatedAt: requirement.UpdatedAt.ToDateTimeOffset());
    }

    internal static Dictionary<string, string?> CaptureRequirementSnapshot(Requirement req)
    {
        return new Dictionary<string, string?>
        {
            ["Title"] = req.Title,
            ["Description"] = req.Description,
            ["Priority"] = req.Priority.ToString(),
            ["ConversationId"] = req.ConversationId?.ToString(),
        };
    }

    internal static List<object> BuildRequirementChanges(
        Dictionary<string, string?> before, Requirement req)
    {
        var after = CaptureRequirementSnapshot(req);
        var changes = new List<object>();
        foreach (var entry in before)
        {
            var bv = entry.Value ?? string.Empty;
            var av = after[entry.Key] ?? string.Empty;
            if (!string.Equals(bv, av, StringComparison.Ordinal))
                changes.Add(new { field = entry.Key, oldValue = entry.Value, newValue = after[entry.Key] });
        }
        return changes;
    }

    internal static RequirementTreeNodeDto BuildTreeNode(
        Requirement requirement,
        Dictionary<GlobalUniqueId, List<Requirement>> childrenMap)
    {
        var children = childrenMap.TryGetValue(requirement.Id, out var childList)
            ? childList.Select(c => BuildTreeNode(c, childrenMap)).ToList()
            : [];

        return new RequirementTreeNodeDto(
            Id: requirement.Id.ToString(),
            Code: requirement.Code,
            Title: requirement.Title,
            Level: requirement.Level,
            Priority: requirement.Priority,
            Status: requirement.Status,
            ParentId: requirement.ParentId?.ToString(),
            ChildrenCount: children.Count,
            Children: children);
    }
}
