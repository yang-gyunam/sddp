using System.Text.Json;
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

public sealed class DiagramHandlers(IUnitOfWork unitOfWork, IDiagramOriginAccess origins, IDiagramListReader lists,
    IStoredDiagramReader models) :
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
        var candidates = new List<(DiagramVersion Version, DiagramOrigin[] Origins)>();
        foreach (var version in versions)
        {
            var inspected = models.Inspect(diagram.Id, version.ModelJson, diagram.Kind);
            if (inspected is { Document.StoredSchemaVersion: 1 }) candidates.Add((version, inspected.Origins));
        }
        var readable = await origins.ReadableOrigins(candidates.SelectMany(v => v.Origins), ct);
        return candidates.Where(v => v.Origins.All(readable.Contains)).Select(v => v.Version)
            .OrderByDescending(v => v.Revision).Select(v => new DiagramVersionListItemDto(
            v.Revision, v.Title, v.ChangeSummary, v.CreatedBy.ToString(), v.CreatedAt.ToDateTimeOffset())).ToArray();
    }

    public async Task<DiagramVersionDto> Handle(GetDiagramVersionQuery request, CancellationToken ct)
    {
        var diagram = await Get(request.TenantId, request.ProjectId, request.DiagramId, ct);
        return Map(await Version(diagram, request.Revision, ct));
    }

    public async Task<DiagramComparisonDto> Handle(CompareDiagramVersionsQuery request, CancellationToken ct)
    {
        var diagram = await Get(request.TenantId, request.ProjectId, request.DiagramId, ct);
        var left = await Version(diagram, request.Left, ct);
        var right = await Version(diagram, request.Right, ct);
        return new DiagramComparisonDto(request.DiagramId.ToString(), request.Left, request.Right,
            DiagramDiff.Compare(Read(left.ModelJson), Read(right.ModelJson)));
    }

    private async Task<Diagram> Get(GlobalUniqueId tenantId, GlobalUniqueId projectId, GlobalUniqueId id, CancellationToken ct)
    {
        var diagram = await unitOfWork.Repository<Diagram>().GetByIdAsync(id, ct);
        if (diagram is null || diagram.TenantId != tenantId || diagram.ProjectId != projectId)
            throw new NotFoundException("Diagram", id.ToString());
        await RequireLegacyModel(diagram, diagram.ModelJson, "Diagram", id.ToString(), ct);
        return diagram;
    }

    private async Task<DiagramVersion> Version(Diagram diagram, int revision, CancellationToken ct)
    {
        var versions = await unitOfWork.Repository<DiagramVersion>().FindAsync(
            v => v.TenantId == diagram.TenantId && v.ProjectId == diagram.ProjectId && v.DiagramId == diagram.Id && v.Revision == revision, ct);
        var revisionId = revision.ToString(System.Globalization.CultureInfo.InvariantCulture);
        var version = versions.SingleOrDefault() ?? throw new NotFoundException("Diagram revision", revisionId);
        await RequireLegacyModel(diagram, version.ModelJson, "Diagram revision", revisionId, ct);
        return version;
    }

    private async Task RequireLegacyModel(Diagram diagram, string modelJson, string entity, string id, CancellationToken ct)
    {
        var inspected = models.Inspect(diagram.Id, modelJson, diagram.Kind) ?? throw new NotFoundException(entity, id);
        await origins.RequireOrigins(inspected.Origins, ct);
        if (inspected.Document.StoredSchemaVersion != 1) throw new DiagramSchemaConflictException();
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
