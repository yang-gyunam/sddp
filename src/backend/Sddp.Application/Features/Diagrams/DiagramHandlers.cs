using System.Text.Json;
using Sddp.Abstractions.Models.Diagrams.V2;
using MediatR;
using Sddp.Abstractions.DTOs;
using Sddp.Abstractions.Exceptions;
using Sddp.Abstractions.Interfaces;
using Sddp.Abstractions.Models.Diagrams;
using Sddp.Abstractions.ValueObjects;
using Sddp.Application.Services;
using Sddp.Application.Requests;
using Sddp.Domain.Entities;

namespace Sddp.Application.Features.Diagrams;

public sealed class DiagramHandlers(IUnitOfWork unitOfWork, IDiagramOriginAccess origins, IDiagramListReader lists) :
    IRequestHandler<CreateDiagramCommand, DiagramDto>,
    IRequestHandler<UpdateDiagramCommand, DiagramDto>,
    IRequestHandler<ListDiagramsQuery, DiagramListDto>,
    IRequestHandler<GetDiagramQuery, DiagramDto>,
    IRequestHandler<ListDiagramVersionsQuery, IReadOnlyList<DiagramVersionListItemDto>>,
    IRequestHandler<GetDiagramVersionQuery, DiagramVersionDto>,
    IRequestHandler<CompareDiagramVersionsQuery, DiagramComparisonDto>
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public async Task<DiagramDto> Handle(CreateDiagramCommand request, CancellationToken ct)
    {
        if (request.Dto.Kind is not ("authored" or "derived"))
            throw new ValidationException("kind", "Expected authored or derived.");
        var id = GlobalUniqueId.NewId();
        var model = Prepare(request.Dto.Model, id, 1);
        await origins.RequireModel(model, request.Dto.Kind, ct);
        var diagram = new Diagram(id, request.TenantId, request.ProjectId, Title(request.Dto.Title),
            request.Dto.Kind, Serialize(model), request.UserId);
        var version = new DiagramVersion(diagram, Summary(request.Dto.ChangeSummary));
        await unitOfWork.Repository<Diagram>().AddAsync(diagram, ct);
        await unitOfWork.Repository<DiagramVersion>().AddAsync(version, ct);
        await unitOfWork.SaveChangesAsync(ct);
        request.AuditLog = new AuditLogRequest(request.UserId, "create", "diagram", id,
            new { diagram.Title, diagram.Revision }, request.TenantId, request.ProjectId);
        return Map(diagram);
    }

    public async Task<DiagramDto> Handle(UpdateDiagramCommand request, CancellationToken ct)
    {
        var diagram = await Get(request.TenantId, request.ProjectId, request.DiagramId, ct);
        if (request.Dto.ExpectedRevision != diagram.Revision)
            throw new ConflictException($"Expected revision {request.Dto.ExpectedRevision}, current revision {diagram.Revision}.");
        if (request.Dto.Model?.DiagramId != diagram.Id.ToString())
            throw new ValidationException("model", "Model diagramId must match the saved diagram.");
        var title = Title(request.Dto.Title);
        var summary = Summary(request.Dto.ChangeSummary);
        var currentCandidate = Prepare(request.Dto.Model, diagram.Id, diagram.Revision);
        await origins.RequireModel(currentCandidate, diagram.Kind, ct);
        var previous = Read(diagram.ModelJson);
        // Compare typed JSON values so dictionary insertion order is irrelevant.
        if (title == diagram.Title && System.Text.Json.Nodes.JsonNode.DeepEquals(
            JsonSerializer.SerializeToNode(previous, Json), JsonSerializer.SerializeToNode(currentCandidate, Json)))
            return Map(diagram);
        var next = currentCandidate with { Revision = checked(diagram.Revision + 1) };
        diagram.Update(title, Serialize(next), request.UserId);
        await unitOfWork.Repository<DiagramVersion>().AddAsync(new DiagramVersion(diagram, summary), ct);
        await unitOfWork.SaveChangesAsync(ct);
        request.AuditLog = new AuditLogRequest(request.UserId, "update", "diagram", diagram.Id,
            new { diagram.Title, diagram.Revision }, request.TenantId, request.ProjectId);
        return Map(diagram);
    }

    public async Task<DiagramListDto> Handle(ListDiagramsQuery request, CancellationToken ct)
    {
        var page = await DiagramListPage.Read(lists, origins, request.TenantId, request.ProjectId, request.Search,
            request.Page, request.PageSize, legacyOnly: true, ct);
        return new(page.Items.Select(d => new DiagramListItemDto(d.Id, d.Title, d.Kind, d.Revision, d.UpdatedAt)).ToArray(),
            page.TotalCount, page.Page, page.PageSize);
    }

    public async Task<DiagramDto> Handle(GetDiagramQuery request, CancellationToken ct) =>
        Map(await Get(request.TenantId, request.ProjectId, request.DiagramId, ct));

    public async Task<IReadOnlyList<DiagramVersionListItemDto>> Handle(ListDiagramVersionsQuery request, CancellationToken ct)
    {
        var diagram = await Get(request.TenantId, request.ProjectId, request.DiagramId, ct);
        var versions = await unitOfWork.Repository<DiagramVersion>().FindAsync(
            v => v.TenantId == request.TenantId && v.ProjectId == request.ProjectId && v.DiagramId == request.DiagramId, ct);
        var readable = new List<DiagramVersion>();
        foreach (var v in versions)
            if (await origins.CanReadModel(Read(v.ModelJson), diagram.Kind, ct)) readable.Add(v);
        return readable.OrderByDescending(v => v.Revision).Select(v => new DiagramVersionListItemDto(
            v.Revision, v.Title, v.ChangeSummary, v.CreatedBy.ToString(), v.CreatedAt.ToDateTimeOffset())).ToArray();
    }

    public async Task<DiagramVersionDto> Handle(GetDiagramVersionQuery request, CancellationToken ct)
    {
        await Get(request.TenantId, request.ProjectId, request.DiagramId, ct);
        return Map(await Version(request.TenantId, request.ProjectId, request.DiagramId, request.Revision, ct));
    }

    public async Task<DiagramComparisonDto> Handle(CompareDiagramVersionsQuery request, CancellationToken ct)
    {
        await Get(request.TenantId, request.ProjectId, request.DiagramId, ct);
        var left = await Version(request.TenantId, request.ProjectId, request.DiagramId, request.Left, ct);
        var right = await Version(request.TenantId, request.ProjectId, request.DiagramId, request.Right, ct);
        return new DiagramComparisonDto(request.DiagramId.ToString(), request.Left, request.Right,
            DiagramDiff.Compare(Read(left.ModelJson), Read(right.ModelJson)));
    }

    private async Task<Diagram> Get(GlobalUniqueId tenantId, GlobalUniqueId projectId, GlobalUniqueId id, CancellationToken ct)
    {
        var diagram = await unitOfWork.Repository<Diagram>().GetByIdAsync(id, ct);
        if (diagram is null || diagram.TenantId != tenantId || diagram.ProjectId != projectId)
            throw new NotFoundException("Diagram", id.ToString());
        var stored = DiagramStorage2.Read(diagram.ModelJson);
        await origins.RequireModel(DiagramStorage2.AccessView(stored.Model), diagram.Kind, ct);
        if (stored.StoredSchemaVersion != 1) throw new DiagramSchemaConflictException();
        return diagram;
    }

    private async Task<DiagramVersion> Version(GlobalUniqueId tenantId, GlobalUniqueId projectId,
        GlobalUniqueId id, int revision, CancellationToken ct)
    {
        var versions = await unitOfWork.Repository<DiagramVersion>().FindAsync(
            v => v.TenantId == tenantId && v.ProjectId == projectId && v.DiagramId == id && v.Revision == revision, ct);
        var version = versions.SingleOrDefault() ?? throw new NotFoundException("Diagram revision", revision.ToString(System.Globalization.CultureInfo.InvariantCulture));
        var diagram = await Get(tenantId, projectId, id, ct);
        await origins.RequireModel(Read(version.ModelJson), diagram.Kind, ct);
        return version;
    }

    private static DiagramRevision Prepare(DiagramRevision? model, GlobalUniqueId id, int revision)
    {
        if (model is null) throw new ValidationException("model", "A diagram model is required.");
        var candidate = model with { DiagramId = id.ToString(), Revision = revision };
        try { DiagramValidation.Validate(candidate); }
        catch (InvalidDataException ex) { throw new ValidationException("model", ex.Message); }
        return candidate;
    }

    internal static string Title(string? title)
    {
        var value = title?.Trim();
        if (value is null || value.Length is < 1 or > 200)
            throw new ValidationException("title", "Title must contain 1 to 200 characters.");
        return value;
    }

    internal static string? Summary(string? summary)
    {
        if (summary?.Length > 500) throw new ValidationException("changeSummary", "Maximum length is 500.");
        return summary;
    }

    private static string Serialize(DiagramRevision model) => JsonSerializer.Serialize(model, Json);
    private static DiagramRevision Read(string json) => JsonSerializer.Deserialize<DiagramRevision>(json, Json)!;
    private static DiagramDto Map(Diagram d) => new(d.Id.ToString(), d.Title, d.Kind, d.Revision,
        Read(d.ModelJson), d.CreatedBy.ToString(), d.UpdatedBy.ToString(), d.CreatedAt.ToDateTimeOffset(), d.UpdatedAt.ToDateTimeOffset());
    private static DiagramVersionDto Map(DiagramVersion v) => new(v.Revision, v.Title, Read(v.ModelJson),
        v.ChangeSummary, v.CreatedBy.ToString(), v.CreatedAt.ToDateTimeOffset());
}
