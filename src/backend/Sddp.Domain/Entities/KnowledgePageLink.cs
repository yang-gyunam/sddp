using Sddp.Abstractions.Base;
using Sddp.Abstractions.Enums;
using Sddp.Abstractions.ValueObjects;

namespace Sddp.Domain.Entities;

public class KnowledgePageLink : EntityBase
{
    public GlobalUniqueId TenantId { get; private set; }
    public GlobalUniqueId ProjectId { get; private set; }
    public GlobalUniqueId PageId { get; private set; }
    public KnowledgeTargetType TargetType { get; private set; }
    public GlobalUniqueId TargetId { get; private set; }

    public KnowledgePage Page { get; private set; } = null!;

    private KnowledgePageLink() { }

    public KnowledgePageLink(
        GlobalUniqueId tenantId,
        GlobalUniqueId projectId,
        GlobalUniqueId pageId,
        KnowledgeTargetType targetType,
        GlobalUniqueId targetId)
    {
        TenantId = tenantId;
        ProjectId = projectId;
        PageId = pageId;
        TargetType = targetType;
        TargetId = targetId;
    }
}
