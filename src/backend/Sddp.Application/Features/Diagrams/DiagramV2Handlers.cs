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

public sealed class DiagramV2Handlers(IUnitOfWork unitOfWork, IDiagramOriginAccess origins, IDiagramListReader lists,
    IStoredDiagramReader models) :
    IRequestHandler<CreateDiagramV2Command, DiagramV2Dto>,
    IRequestHandler<UpdateDiagramV2Command, DiagramV2Dto>,
    IRequestHandler<ListDiagramsV2Query, DiagramV2ListDto>,
    IRequestHandler<GetDiagramV2Query, DiagramV2Dto>,
    IRequestHandler<ListDiagramVersionsV2Query, IReadOnlyList<DiagramVersionListItemDto>>,
    IRequestHandler<GetDiagramVersionV2Query, DiagramV2VersionDto>,
    IRequestHandler<CompareDiagramVersionsV2Query, DiagramV2ComparisonDto>
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public async Task<DiagramV2Dto> Handle(CreateDiagramV2Command request, CancellationToken ct)
    {
        if (request.Dto.Kind is not ("authored" or "derived"))
            throw new ValidationException("kind", "Expected authored or derived.");
        var id = GlobalUniqueId.NewId();
        var model = Prepare(request.Dto.Model, id, 1);
        await RequireAccess(model, request.Dto.Kind, ct);
        var diagram = new Diagram(id, request.TenantId, request.ProjectId, DiagramHandlers.Title(request.Dto.Title),
            request.Dto.Kind, Serialize(model), request.UserId);
        var version = new DiagramVersion(diagram, DiagramHandlers.Summary(request.Dto.ChangeSummary));
        await unitOfWork.Repository<Diagram>().AddAsync(diagram, ct);
        await unitOfWork.Repository<DiagramVersion>().AddAsync(version, ct);
        await unitOfWork.SaveChangesAsync(ct);
        request.AuditLog = new AuditLogRequest(request.UserId, "create", "diagram", id,
            new { diagram.Title, diagram.Revision }, request.TenantId, request.ProjectId);
        return Map(diagram, DiagramStorage2.Read(diagram.ModelJson));
    }

    public async Task<DiagramV2Dto> Handle(UpdateDiagramV2Command request, CancellationToken ct)
    {
        var (diagram, stored) = await Get(request.TenantId, request.ProjectId, request.DiagramId, ct);
        if (request.Dto.ExpectedRevision != diagram.Revision)
            throw new ConflictException($"Expected revision {request.Dto.ExpectedRevision}, current revision {diagram.Revision}.");
        if (request.Dto.Model?.DiagramId != diagram.Id.ToString())
            throw new ValidationException("model", "Model diagramId must match the saved diagram.");
        var title = DiagramHandlers.Title(request.Dto.Title);
        var summary = DiagramHandlers.Summary(request.Dto.ChangeSummary);
        var currentCandidate = Prepare(request.Dto.Model, diagram.Id, diagram.Revision);
        await RequireAccess(currentCandidate, diagram.Kind, ct);
        var previous = stored.Model;
        try { _ = DiagramDiff2.Compare(previous, currentCandidate); }
        catch (InvalidDataException ex) { throw new ValidationException("model", ex.Message); }
        // Compare typed JSON values so dictionary insertion order is irrelevant.
        if (stored.StoredSchemaVersion == 2 && title == diagram.Title && System.Text.Json.Nodes.JsonNode.DeepEquals(
            JsonSerializer.SerializeToNode(previous, Json), JsonSerializer.SerializeToNode(currentCandidate, Json)))
            return Map(diagram, stored);
        var next = currentCandidate with { Revision = checked(diagram.Revision + 1) };
        diagram.Update(title, Serialize(next), request.UserId);
        await unitOfWork.Repository<DiagramVersion>().AddAsync(new DiagramVersion(diagram, summary), ct);
        await unitOfWork.SaveChangesAsync(ct);
        request.AuditLog = new AuditLogRequest(request.UserId, "update", "diagram", diagram.Id,
            new { diagram.Title, diagram.Revision }, request.TenantId, request.ProjectId);
        return Map(diagram, DiagramStorage2.Read(diagram.ModelJson));
    }

    public async Task<DiagramV2ListDto> Handle(ListDiagramsV2Query request, CancellationToken ct)
    {
        return await DiagramListPage.Read(lists, origins, request.TenantId, request.ProjectId, request.Search,
            request.Page, request.PageSize, legacyOnly: false, ct);
    }

    public async Task<DiagramV2Dto> Handle(GetDiagramV2Query request, CancellationToken ct)
    {
        var (diagram, stored) = await Get(request.TenantId, request.ProjectId, request.DiagramId, ct);
        return Map(diagram, stored);
    }

    public async Task<IReadOnlyList<DiagramVersionListItemDto>> Handle(ListDiagramVersionsV2Query request, CancellationToken ct)
    {
        var (diagram, _) = await Get(request.TenantId, request.ProjectId, request.DiagramId, ct);
        var versions = await unitOfWork.Repository<DiagramVersion>().FindAsync(
            v => v.TenantId == request.TenantId && v.ProjectId == request.ProjectId && v.DiagramId == request.DiagramId, ct);
        var candidates = new List<(DiagramVersion Version, DiagramOrigin[] Origins)>();
        foreach (var version in versions)
        {
            var inspected = models.Inspect(diagram.Id, version.ModelJson, diagram.Kind);
            if (inspected is not null) candidates.Add((version, inspected.Origins));
        }
        var readable = await origins.ReadableOrigins(candidates.SelectMany(v => v.Origins), ct);
        return candidates.Where(v => v.Origins.All(readable.Contains)).Select(v => v.Version)
            .OrderByDescending(v => v.Revision).Select(v => new DiagramVersionListItemDto(
                v.Revision, v.Title, v.ChangeSummary, v.CreatedBy.ToString(), v.CreatedAt.ToDateTimeOffset())).ToArray();
    }

    public async Task<DiagramV2VersionDto> Handle(GetDiagramVersionV2Query request, CancellationToken ct)
    {
        var (diagram, _) = await Get(request.TenantId, request.ProjectId, request.DiagramId, ct);
        var (version, stored) = await Version(diagram, request.Revision, ct);
        return new(version.Revision, version.Title, stored.StoredSchemaVersion, stored.Model, version.ChangeSummary,
            version.CreatedBy.ToString(), version.CreatedAt.ToDateTimeOffset());
    }

    public async Task<DiagramV2ComparisonDto> Handle(CompareDiagramVersionsV2Query request, CancellationToken ct)
    {
        var (diagram, _) = await Get(request.TenantId, request.ProjectId, request.DiagramId, ct);
        var (_, left) = await Version(diagram, request.Left, ct);
        var (_, right) = await Version(diagram, request.Right, ct);
        var diff = DiagramDiff2.Compare(left.Model, right.Model);
        return new DiagramV2ComparisonDto(request.DiagramId.ToString(), request.Left, request.Right, diff.Changes, diff.ProvenanceChanges);
    }

    private async Task<(Diagram, StoredDiagramModel2)> Get(GlobalUniqueId tenantId, GlobalUniqueId projectId,
        GlobalUniqueId id, CancellationToken ct)
    {
        var diagram = await unitOfWork.Repository<Diagram>().GetByIdAsync(id, ct);
        if (diagram is null || diagram.TenantId != tenantId || diagram.ProjectId != projectId)
            throw new NotFoundException("Diagram", id.ToString());
        var inspected = models.Inspect(id, diagram.ModelJson, diagram.Kind) ?? throw new NotFoundException("Diagram", id.ToString());
        await origins.RequireOrigins(inspected.Origins, ct);
        return (diagram, inspected.Document);
    }

    private async Task<(DiagramVersion, StoredDiagramModel2)> Version(Diagram diagram, int revision, CancellationToken ct)
    {
        var versions = await unitOfWork.Repository<DiagramVersion>().FindAsync(
            v => v.TenantId == diagram.TenantId && v.ProjectId == diagram.ProjectId && v.DiagramId == diagram.Id && v.Revision == revision, ct);
        var version = versions.SingleOrDefault();
        var inspected = version is null ? null : models.Inspect(diagram.Id, version.ModelJson, diagram.Kind);
        if (version is null || inspected is null)
            throw new NotFoundException("Diagram revision", revision.ToString(System.Globalization.CultureInfo.InvariantCulture));
        await origins.RequireOrigins(inspected.Origins, ct);
        return (version, inspected.Document);
    }

    private async Task RequireAccess(DiagramRevision2 model, string? kind, CancellationToken ct)
    {
        try { await origins.RequireModel(DiagramStorage2.AccessView(model), kind, ct); }
        catch (InvalidDataException ex) { throw new ValidationException("model", ex.Message); }
    }

    private static DiagramRevision2 Prepare(DiagramRevision2? model, GlobalUniqueId id, int revision)
    {
        if (model is null) throw new ValidationException("model", "A diagram model is required.");
        var candidate = model with { DiagramId = id.ToString(), Revision = revision };
        try { DiagramValidation2.Validate(candidate); }
        catch (InvalidDataException ex) { throw new ValidationException("model", ex.Message); }
        return candidate;
    }

    private static string Serialize(DiagramRevision2 model) => DiagramStorage2.Write(model);
    private static DiagramV2Dto Map(Diagram d, StoredDiagramModel2 stored) =>
        new(d.Id.ToString(), d.Title, d.Kind, d.Revision, stored.StoredSchemaVersion, stored.Model,
            d.CreatedBy.ToString(), d.UpdatedBy.ToString(), d.CreatedAt.ToDateTimeOffset(), d.UpdatedAt.ToDateTimeOffset());
}
