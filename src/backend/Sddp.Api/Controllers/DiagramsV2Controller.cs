using MediatR;
using Sddp.Application.Services;
using Sddp.Abstractions.Models.Diagrams.V2;
using YamlDotNet.Core;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sddp.Abstractions.Constants;
using Sddp.Abstractions.DTOs;
using Sddp.Abstractions.ValueObjects;
using Sddp.Api.Authorization;
using Sddp.Application.Features.Diagrams;
using Sddp.Domain.Entities;
using Sddp.Abstractions.Interfaces;
using Sddp.Abstractions.Exceptions;
using Sddp.Infrastructure.Services.Diagrams;

namespace Sddp.Api.Controllers;

/// <summary>Project-shared diagrams and immutable revisions.</summary>
[Authorize]
[Route("api/v2/projects/{projectId}/diagrams")]
[ProducesResponseType(StatusCodes.Status400BadRequest)]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
[ProducesResponseType(StatusCodes.Status403Forbidden)]
[ProducesResponseType(StatusCodes.Status404NotFound)]
[ProducesResponseType(StatusCodes.Status409Conflict)]
public sealed class DiagramsV2Controller(ISender sender, DiagramProjection projection, IDiagramOriginAccess origins, IEnumerable<IDiagramParser2> parsers) : BaseApiController
{
    [HttpGet]
    [ResourceScope<Project>("projectId")]
    [RequirePermission(PermissionCodes.DiagramRead)]
    [ProducesResponseType(typeof(ApiResponse<DiagramV2ListDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> List(Guid projectId, int page = 1, int pageSize = 50, string? search = null, CancellationToken ct = default)
    {
        var error = Scope(projectId, out var tenant, out var project, out _);
        if (error is not null) return error;
        return Ok(ApiResponse<DiagramV2ListDto>.Ok(await sender.Send(new ListDiagramsV2Query(tenant, project, page, pageSize, search), ct)));
    }

    [HttpGet("{diagramId:guid}")]
    [ResourceScope<Project>("projectId")]
    [RequirePermission(PermissionCodes.DiagramRead)]
    [ProducesResponseType(typeof(ApiResponse<DiagramV2Dto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Get(Guid projectId, Guid diagramId, CancellationToken ct)
    {
        var error = Scope(projectId, out var tenant, out var project, out _);
        if (error is not null) return error;
        return Ok(ApiResponse<DiagramV2Dto>.Ok(await sender.Send(new GetDiagramV2Query(tenant, project, GlobalUniqueId.FromGuid(diagramId)), ct)));
    }

    [HttpPost]
    [ResourceScope<Project>("projectId")]
    [RequirePermission(PermissionCodes.DiagramCreate)]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<DiagramV2Dto>), StatusCodes.Status201Created)]
    public async Task<IActionResult> Create(Guid projectId, [FromBody] CreateDiagramV2Dto dto, CancellationToken ct)
    {
        var error = Scope(projectId, out var tenant, out var project, out var user);
        if (error is not null) return error;
        var result = await sender.Send(new CreateDiagramV2Command(tenant, project, user, dto), ct);
        return Created($"/api/v2/projects/{projectId}/diagrams/{result.Id}", ApiResponse<DiagramV2Dto>.Ok(result));
    }

    [HttpPut("{diagramId:guid}")]
    [ResourceScope<Project>("projectId")]
    [RequirePermission(PermissionCodes.DiagramUpdate)]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<DiagramV2Dto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Update(Guid projectId, Guid diagramId, [FromBody] UpdateDiagramV2Dto dto, CancellationToken ct)
    {
        var error = Scope(projectId, out var tenant, out var project, out var user);
        if (error is not null) return error;
        var result = await sender.Send(new UpdateDiagramV2Command(tenant, project, GlobalUniqueId.FromGuid(diagramId), user, dto), ct);
        return Ok(ApiResponse<DiagramV2Dto>.Ok(result));
    }

    [HttpGet("{diagramId:guid}/versions")]
    [ResourceScope<Project>("projectId")]
    [RequirePermission(PermissionCodes.DiagramRead)]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<DiagramVersionListItemDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Versions(Guid projectId, Guid diagramId, CancellationToken ct)
    {
        var error = Scope(projectId, out var tenant, out var project, out _);
        if (error is not null) return error;
        var result = await sender.Send(new ListDiagramVersionsV2Query(tenant, project, GlobalUniqueId.FromGuid(diagramId)), ct);
        return Ok(ApiResponse<IReadOnlyList<DiagramVersionListItemDto>>.Ok(result));
    }

    [HttpGet("{diagramId:guid}/versions/{revision:int}")]
    [ResourceScope<Project>("projectId")]
    [RequirePermission(PermissionCodes.DiagramRead)]
    [ProducesResponseType(typeof(ApiResponse<DiagramV2VersionDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Version(Guid projectId, Guid diagramId, int revision, CancellationToken ct)
    {
        var error = Scope(projectId, out var tenant, out var project, out _);
        if (error is not null) return error;
        var result = await sender.Send(new GetDiagramVersionV2Query(tenant, project, GlobalUniqueId.FromGuid(diagramId), revision), ct);
        return Ok(ApiResponse<DiagramV2VersionDto>.Ok(result));
    }

    [HttpGet("{diagramId:guid}/compare")]
    [ResourceScope<Project>("projectId")]
    [RequirePermission(PermissionCodes.DiagramRead)]
    [ProducesResponseType(typeof(ApiResponse<DiagramV2ComparisonDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Compare(Guid projectId, Guid diagramId, int left, int right, CancellationToken ct)
    {
        var error = Scope(projectId, out var tenant, out var project, out _);
        if (error is not null) return error;
        var result = await sender.Send(new CompareDiagramVersionsV2Query(tenant, project, GlobalUniqueId.FromGuid(diagramId), left, right), ct);
        return Ok(ApiResponse<DiagramV2ComparisonDto>.Ok(result));
    }

    /// <summary>Validate YAML text or original file bytes without saving a revision.</summary>
    [HttpPost("yaml/decode")]
    [Authorize]
    [ResourceScope<Project>("projectId")]
    [RequirePermission(PermissionCodes.DiagramRead)]
    [ProducesResponseType(typeof(ApiResponse<DiagramV2ModelDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Decode(Guid projectId, [FromBody] DecodeDiagramYamlDto dto, CancellationToken ct)
    {
        var error = Scope(projectId, out _, out _, out _);
        if (error is not null) return error;
        if ((dto.Text is null) == (dto.ContentBase64 is null))
            throw new ValidationException("yaml", "Provide exactly one of text or contentBase64.");
        try
        {
            if (dto.Text?.Length > 2 * 1024 * 1024 || dto.ContentBase64?.Length > 2800000)
                throw new InvalidDataException("Maximum YAML size is 2 MiB.");
            var text = dto.Text;
            if (text is null)
            {
                var bytes = Convert.FromBase64String(dto.ContentBase64!);
                if (bytes.Length > 2 * 1024 * 1024) throw new InvalidDataException("Maximum YAML size is 2 MiB.");
                text = DiagramFile.Decode(bytes);
            }
            if (System.Text.Encoding.UTF8.GetByteCount(text) > 2 * 1024 * 1024)
                throw new InvalidDataException("Maximum YAML size is 2 MiB.");
            var model = DiagramYaml2.ReadAny(text, out var inputSchemaVersion);
            await origins.RequireModel(DiagramStorage2.AccessView(model), null, ct);
            return Ok(ApiResponse<DiagramV2ModelDto>.Ok(new(model, inputSchemaVersion)));
        }
        catch (Exception ex) when (ex is InvalidDataException or FormatException or YamlException or System.Text.Json.JsonException)
        { throw new ValidationException("yaml", ex.Message); }
    }

    /// <summary>Encode a common model using the canonical YAML codec without saving.</summary>
    [HttpPost("yaml/encode")]
    [Authorize]
    [ResourceScope<Project>("projectId")]
    [RequirePermission(PermissionCodes.DiagramRead)]
    [ProducesResponseType(typeof(ApiResponse<DiagramYamlDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Encode(Guid projectId, [FromBody] DiagramV2ModelDto dto, CancellationToken ct)
    {
        var error = Scope(projectId, out _, out _, out _);
        if (error is not null) return error;
        try
        {
            await origins.RequireModel(DiagramStorage2.AccessView(dto.Model), null, ct);
            return Ok(ApiResponse<DiagramYamlDto>.Ok(new(DiagramYaml2.Write(dto.Model))));
        }
        catch (InvalidDataException ex) { throw new ValidationException("model", ex.Message); }
    }

    /// <summary>Preview selected current entities and relationships without saving a revision.</summary>
    [HttpPost("projection")]
    [Authorize]
    [ResourceScope<Project>("projectId")]
    [RequirePermission(PermissionCodes.DiagramRead)]
    [ProducesResponseType(typeof(ApiResponse<DiagramV2ModelDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Project(Guid projectId, [FromBody] DiagramProjectionDto dto, CancellationToken ct)
    {
        var error = Scope(projectId, out var tenant, out var project, out _);
        if (error is not null) return error;
        return Ok(ApiResponse<DiagramV2ModelDto>.Ok(new(DiagramBridge2.Promote(await projection.Build(tenant, project, dto, ct)))));
    }

    /// <summary>Registered SDDP parser adapters and their supported source profiles.</summary>
    [HttpGet("parsers")]
    [ResourceScope<Project>("projectId")]
    [RequirePermission(PermissionCodes.DiagramRead)]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<DiagramParserCapability2>>), StatusCodes.Status200OK)]
    public IActionResult Parsers(Guid projectId)
    {
        var error = Scope(projectId, out _, out _, out _);
        if (error is not null) return error;
        return Ok(ApiResponse<IReadOnlyList<DiagramParserCapability2>>.Ok(parsers.Select(p => p.Capability).ToArray()));
    }

    /// <summary>Preview source, identity decisions and structural changes without persisting.</summary>
    [HttpPost("source/preview")]
    [Authorize]
    [ResourceScope<Project>("projectId")]
    [RequirePermission(PermissionCodes.DiagramRead)]
    [ProducesResponseType(typeof(ApiResponse<DiagramIdentityPreview2>), StatusCodes.Status200OK)]
    public async Task<IActionResult> PreviewSource(Guid projectId, [FromBody] DiagramSourcePreviewDto dto, CancellationToken ct)
    {
        var error = Scope(projectId, out var tenant, out var project, out _);
        if (error is not null) return error;
        if (dto.Text is null || System.Text.Encoding.UTF8.GetByteCount(dto.Text) > 2 * 1024 * 1024)
            throw new ValidationException("source", "Expected source text of at most 2 MiB.");
        var baseline = dto.Baseline;
        if (dto.DiagramId is { } savedId)
        {
            var saved = await sender.Send(new GetDiagramV2Query(tenant, project, GlobalUniqueId.FromGuid(savedId)), ct);
            if (saved.Kind != "authored") throw new ValidationException("source", "Derived diagrams retain their projection provenance.");
            if (dto.ExpectedRevision != saved.Revision) throw new ConflictException("Source preview requires the current saved revision.");
            baseline ??= saved.Model;
            if (baseline.DiagramId != saved.Id || baseline.Revision != saved.Revision)
                throw new ValidationException("baseline", "Baseline document/revision must match the saved target.");
            try { _ = DiagramDiff2.Compare(saved.Model, baseline); }
            catch (InvalidDataException ex) { throw new ValidationException("baseline", ex.Message); }
        }
        else if (dto.ExpectedRevision is not null) throw new ValidationException("expectedRevision", "A saved target is required.");
        try
        {
            if (baseline is not null) await origins.RequireModel(DiagramStorage2.AccessView(baseline), "authored", ct);
            var parser = parsers.SingleOrDefault(p => p.Capability.Id == dto.AdapterId);
            if (parser is null) return Ok(ApiResponse<DiagramIdentityPreview2>.Ok(new("", null,
                [new("UNSUPPORTED_CAPABILITY", "error", "Parser adapter is unavailable.")], [], [], null)));
            var result = DiagramIdentityService2.Preview(parser, dto.Text, dto.Profile,
                baseline?.DiagramId ?? GlobalUniqueId.NewId().ToString(), baseline?.Revision ?? 1, baseline, dto.Resolution, ct);
            return Ok(ApiResponse<DiagramIdentityPreview2>.Ok(result));
        }
        catch (InvalidDataException ex) { throw new ValidationException("source", ex.Message); }
    }

    private IActionResult? Scope(Guid routeProject, out GlobalUniqueId tenant, out GlobalUniqueId project, out GlobalUniqueId user)
    {
        user = default;
        var error = RequireTenantAndProject(Request.Headers["X-Tenant-Id"], Request.Headers["X-Project-Id"], out tenant, out project,
            "INVALID_DIAGRAM_SCOPE", "Valid X-Tenant-Id and X-Project-Id headers are required.");
        if (error is not null) return error;
        if (routeProject != project.ToGuid())
            return BadRequest(ApiResponse<object>.Fail("PROJECT_SCOPE_MISMATCH", "Route projectId must match X-Project-Id."));
        return RequireUserId(out user, unauthorizedIfMissing: true);
    }
}
