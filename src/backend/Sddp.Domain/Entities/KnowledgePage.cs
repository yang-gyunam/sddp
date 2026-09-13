using Sddp.Abstractions.Base;
using Sddp.Abstractions.Enums;
using Sddp.Abstractions.ValueObjects;

namespace Sddp.Domain.Entities;

public class KnowledgePage : AuditableEntityBase
{
    public GlobalUniqueId TenantId { get; private set; }
    public GlobalUniqueId ProjectId { get; private set; }
    public GlobalUniqueId? ParentId { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public string Slug { get; private set; } = string.Empty;
    public string ContentJson { get; private set; } = """{"type":"doc","content":[]}""";
    public string ContentText { get; private set; } = string.Empty;
    public string TagsJson { get; private set; } = "[]";
    public KnowledgePageStatus Status { get; private set; } = KnowledgePageStatus.Draft;
    public int Position { get; private set; }
    public int Revision { get; private set; } = 1;
    public GlobalUniqueId? PublishedBy { get; private set; }
    public Timestamp? PublishedAt { get; private set; }
    public Timestamp? ArchivedAt { get; private set; }

    public KnowledgePage? Parent { get; private set; }
    public ICollection<KnowledgePage> Children { get; private set; } = [];
    public ICollection<KnowledgePageVersion> Versions { get; private set; } = [];
    public ICollection<KnowledgePageLink> Links { get; private set; } = [];

    private KnowledgePage() { }

    public KnowledgePage(
        GlobalUniqueId tenantId,
        GlobalUniqueId projectId,
        GlobalUniqueId? parentId,
        string title,
        string slug,
        string contentJson,
        string contentText,
        string tagsJson,
        int position,
        GlobalUniqueId createdBy)
    {
        TenantId = tenantId;
        ProjectId = projectId;
        ParentId = parentId;
        Title = title;
        Slug = slug;
        ContentJson = contentJson;
        ContentText = contentText;
        TagsJson = tagsJson;
        Position = position;
        Status = KnowledgePageStatus.Draft;
        Revision = 1;
        SetCreatedBy(createdBy);
    }

    public void UpdateContent(
        string title,
        string slug,
        string contentJson,
        string contentText,
        string tagsJson,
        GlobalUniqueId updatedBy)
    {
        Title = title;
        Slug = slug;
        ContentJson = contentJson;
        ContentText = contentText;
        TagsJson = tagsJson;
        Revision++;
        SetUpdatedBy(updatedBy);
    }

    public void Move(
        GlobalUniqueId? parentId,
        int position,
        GlobalUniqueId updatedBy,
        bool incrementRevision = true)
    {
        ParentId = parentId;
        Position = position;
        if (incrementRevision)
        {
            Revision++;
        }
        SetUpdatedBy(updatedBy);
    }

    public void Publish(GlobalUniqueId publishedBy)
    {
        Status = KnowledgePageStatus.Published;
        PublishedBy = publishedBy;
        PublishedAt = Timestamp.Now;
        ArchivedAt = null;
        Revision++;
        SetUpdatedBy(publishedBy);
    }

    public void Archive(GlobalUniqueId archivedBy)
    {
        Status = KnowledgePageStatus.Archived;
        ArchivedAt = Timestamp.Now;
        Revision++;
        SetUpdatedBy(archivedBy);
    }

    public void Restore(GlobalUniqueId restoredBy)
    {
        Status = PublishedAt.HasValue
            ? KnowledgePageStatus.Published
            : KnowledgePageStatus.Draft;
        ArchivedAt = null;
        Revision++;
        SetUpdatedBy(restoredBy);
    }

    public void RestoreVersion(
        KnowledgePageVersion version,
        GlobalUniqueId restoredBy)
    {
        Title = version.Title;
        Slug = version.Slug;
        ContentJson = version.ContentJson;
        ContentText = version.ContentText;
        TagsJson = version.TagsJson;
        Revision++;
        SetUpdatedBy(restoredBy);
    }
}
