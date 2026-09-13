using Sddp.Abstractions.Base;
using Sddp.Abstractions.Enums;
using Sddp.Abstractions.ValueObjects;

namespace Sddp.Domain.Entities;

public class KnowledgePageVersion : EntityBase
{
    public GlobalUniqueId TenantId { get; private set; }
    public GlobalUniqueId ProjectId { get; private set; }
    public GlobalUniqueId PageId { get; private set; }
    public int Revision { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public string Slug { get; private set; } = string.Empty;
    public string ContentJson { get; private set; } = """{"type":"doc","content":[]}""";
    public string ContentText { get; private set; } = string.Empty;
    public string TagsJson { get; private set; } = "[]";
    public KnowledgePageStatus Status { get; private set; }
    public string? ChangeSummary { get; private set; }
    public GlobalUniqueId CreatedBy { get; private set; }

    public KnowledgePage Page { get; private set; } = null!;

    private KnowledgePageVersion() { }

    public KnowledgePageVersion(
        KnowledgePage page,
        GlobalUniqueId createdBy,
        string? changeSummary)
    {
        TenantId = page.TenantId;
        ProjectId = page.ProjectId;
        PageId = page.Id;
        Revision = page.Revision;
        Title = page.Title;
        Slug = page.Slug;
        ContentJson = page.ContentJson;
        ContentText = page.ContentText;
        TagsJson = page.TagsJson;
        Status = page.Status;
        ChangeSummary = changeSummary;
        CreatedBy = createdBy;
    }
}
