using System.Text.Json;
using Sddp.Abstractions.DTOs;
using Sddp.Domain.Entities;

namespace Sddp.Application.Features.Knowledge;

public static class KnowledgeMapping
{
    public static KnowledgePageDto ToDto(KnowledgePage page) =>
        new(
            page.Id.ToString(),
            page.TenantId.ToString(),
            page.ProjectId.ToString(),
            page.ParentId?.ToString(),
            page.Title,
            page.Slug,
            ParseJson(page.ContentJson),
            KnowledgeContentParser.DeserializeTags(page.TagsJson),
            page.Status.ToString(),
            page.Position,
            page.Revision,
            page.CreatedBy.ToString(),
            page.UpdatedBy.ToString(),
            page.PublishedBy?.ToString(),
            page.PublishedAt?.ToDateTimeOffset(),
            page.ArchivedAt?.ToDateTimeOffset(),
            page.CreatedAt.ToDateTimeOffset(),
            page.UpdatedAt.ToDateTimeOffset());

    public static KnowledgePageTreeItemDto ToTreeItem(
        KnowledgePage page,
        bool hasChildren) =>
        new(
            page.Id.ToString(),
            page.ParentId?.ToString(),
            page.Title,
            page.Slug,
            page.Status.ToString(),
            page.Position,
            page.Revision,
            hasChildren,
            page.UpdatedBy.ToString(),
            page.UpdatedAt.ToDateTimeOffset());

    public static KnowledgePageVersionSummaryDto ToVersionSummary(KnowledgePageVersion version) =>
        new(
            version.Id.ToString(),
            version.Revision,
            version.Title,
            version.Status.ToString(),
            version.ChangeSummary,
            version.CreatedBy.ToString(),
            version.CreatedAt.ToDateTimeOffset());

    public static KnowledgePageVersionDto ToVersionDto(KnowledgePageVersion version) =>
        new(
            version.Id.ToString(),
            version.PageId.ToString(),
            version.Revision,
            version.Title,
            version.Slug,
            ParseJson(version.ContentJson),
            KnowledgeContentParser.DeserializeTags(version.TagsJson),
            version.Status.ToString(),
            version.ChangeSummary,
            version.CreatedBy.ToString(),
            version.CreatedAt.ToDateTimeOffset());

    public static KnowledgeSearchResultDto ToSearchResult(KnowledgePage page)
    {
        var summary = page.ContentText.Length <= 240
            ? page.ContentText
            : $"{page.ContentText[..237]}...";
        return new KnowledgeSearchResultDto(
            page.Id.ToString(),
            page.ParentId?.ToString(),
            page.Title,
            page.Slug,
            summary,
            page.Status.ToString(),
            KnowledgeContentParser.DeserializeTags(page.TagsJson),
            page.Revision,
            page.UpdatedBy.ToString(),
            page.UpdatedAt.ToDateTimeOffset());
    }

    private static JsonElement ParseJson(string json)
    {
        using var document = JsonDocument.Parse(json);
        return document.RootElement.Clone();
    }
}
