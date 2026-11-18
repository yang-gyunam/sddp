using Sddp.Abstractions.Base;
using Sddp.Abstractions.ValueObjects;

namespace Sddp.Domain.Entities;

public sealed class Diagram : AuditableEntityBase
{
    public GlobalUniqueId TenantId { get; private set; }
    public GlobalUniqueId ProjectId { get; private set; }
    public string Title { get; private set; } = "";
    public string Kind { get; private set; } = "";
    public int Revision { get; private set; } = 1;
    public string ModelJson { get; private set; } = "";

    private Diagram() { }

    public Diagram(GlobalUniqueId id, GlobalUniqueId tenantId, GlobalUniqueId projectId,
        string title, string kind, string modelJson, GlobalUniqueId userId)
    {
        Id = id;
        TenantId = tenantId;
        ProjectId = projectId;
        Title = title;
        Kind = kind;
        ModelJson = modelJson;
        SetCreatedBy(userId);
    }

    public void Update(string title, string modelJson, GlobalUniqueId userId)
    {
        Title = title;
        ModelJson = modelJson;
        Revision++;
        SetUpdatedBy(userId);
    }
}

public sealed class DiagramVersion : EntityBase
{
    public GlobalUniqueId TenantId { get; private set; }
    public GlobalUniqueId ProjectId { get; private set; }
    public GlobalUniqueId DiagramId { get; private set; }
    public int Revision { get; private set; }
    public string Title { get; private set; } = "";
    public string ModelJson { get; private set; } = "";
    public string? ChangeSummary { get; private set; }
    public GlobalUniqueId CreatedBy { get; private set; }

    private DiagramVersion() { }

    public DiagramVersion(Diagram diagram, string? changeSummary)
    {
        TenantId = diagram.TenantId;
        ProjectId = diagram.ProjectId;
        DiagramId = diagram.Id;
        Revision = diagram.Revision;
        Title = diagram.Title;
        ModelJson = diagram.ModelJson;
        ChangeSummary = changeSummary;
        CreatedBy = diagram.UpdatedBy;
    }
}
