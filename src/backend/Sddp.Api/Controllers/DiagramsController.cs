using MediatR;
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
[Route("api/projects/{projectId}/diagrams")]
[ProducesResponseType(StatusCodes.Status400BadRequest)]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
[ProducesResponseType(StatusCodes.Status403Forbidden)]
[ProducesResponseType(StatusCodes.Status404NotFound)]
[ProducesResponseType(StatusCodes.Status409Conflict)]
public sealed class DiagramsController(ISender sender, DiagramProjection projection, IDiagramOriginAccess origins) : BaseApiController
{
    [HttpGet]
    [ResourceScope<Project>("projectId")]
    [RequirePermission(PermissionCodes.DiagramRead)]
    [ProducesResponseType(typeof(ApiResponse<DiagramListDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> List(Guid projectId, int page = 1, int pageSize = 50, string? search = null, CancellationToken ct = default)
    {
        var error = Scope(projectId, out var tenant, out var project, out _);
        if (error is not null) return error;
        return Ok(ApiResponse<DiagramListDto>.Ok(await sender.Send(new ListDiagramsQuery(tenant, project, page, pageSize, search), ct)));
    }

    [HttpGet("{diagramId:guid}")]
    [ResourceScope<Project>("projectId")]
    [RequirePermission(PermissionCodes.DiagramRead)]
    [ProducesResponseType(typeof(ApiResponse<DiagramDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Get(Guid projectId, Guid diagramId, CancellationToken ct)
    {
        var error = Scope(projectId, out var tenant, out var project, out _);
        if (error is not null) return error;
        return Ok(ApiResponse<DiagramDto>.Ok(await sender.Send(new GetDiagramQuery(tenant, project, GlobalUniqueId.FromGuid(diagramId)), ct)));
    }

    [HttpPost]
    [ResourceScope<Project>("projectId")]
    [RequirePermission(PermissionCodes.DiagramCreate)]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<DiagramDto>), StatusCodes.Status201Created)]
    public async Task<IActionResult> Create(Guid projectId, [FromBody] CreateDiagramDto dto, CancellationToken ct)
    {
        var error = Scope(projectId, out var tenant, out var project, out var user);
        if (error is not null) return error;
        var result = await sender.Send(new CreateDiagramCommand(tenant, project, user, dto), ct);
        return Created($"/api/projects/{projectId}/diagrams/{result.Id}", ApiResponse<DiagramDto>.Ok(result));
    }

    [HttpPut("{diagramId:guid}")]
    [ResourceScope<Project>("projectId")]
    [RequirePermission(PermissionCodes.DiagramUpdate)]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<DiagramDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Update(Guid projectId, Guid diagramId, [FromBody] UpdateDiagramDto dto, CancellationToken ct)
    {
        var error = Scope(projectId, out var tenant, out var project, out var user);
        if (error is not null) return error;
        var result = await sender.Send(new UpdateDiagramCommand(tenant, project, GlobalUniqueId.FromGuid(diagramId), user, dto), ct);
        return Ok(ApiResponse<DiagramDto>.Ok(result));
    }

    [HttpGet("{diagramId:guid}/versions")]
    [ResourceScope<Project>("projectId")]
    [RequirePermission(PermissionCodes.DiagramRead)]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<DiagramVersionListItemDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Versions(Guid projectId, Guid diagramId, CancellationToken ct)
    {
        var error = Scope(projectId, out var tenant, out var project, out _);
        if (error is not null) return error;
        var result = await sender.Send(new ListDiagramVersionsQuery(tenant, project, GlobalUniqueId.FromGuid(diagramId)), ct);
        return Ok(ApiResponse<IReadOnlyList<DiagramVersionListItemDto>>.Ok(result));
    }

    [HttpGet("{diagramId:guid}/versions/{revision:int}")]
    [ResourceScope<Project>("projectId")]
    [RequirePermission(PermissionCodes.DiagramRead)]
    [ProducesResponseType(typeof(ApiResponse<DiagramVersionDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Version(Guid projectId, Guid diagramId, int revision, CancellationToken ct)
    {
        var error = Scope(projectId, out var tenant, out var project, out _);
        if (error is not null) return error;
        var result = await sender.Send(new GetDiagramVersionQuery(tenant, project, GlobalUniqueId.FromGuid(diagramId), revision), ct);
        return Ok(ApiResponse<DiagramVersionDto>.Ok(result));
    }

    [HttpGet("{diagramId:guid}/compare")]
    [ResourceScope<Project>("projectId")]
    [RequirePermission(PermissionCodes.DiagramRead)]
    [ProducesResponseType(typeof(ApiResponse<DiagramComparisonDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Compare(Guid projectId, Guid diagramId, int left, int right, CancellationToken ct)
    {
        var error = Scope(projectId, out var tenant, out var project, out _);
        if (error is not null) return error;
        var result = await sender.Send(new CompareDiagramVersionsQuery(tenant, project, GlobalUniqueId.FromGuid(diagramId), left, right), ct);
        return Ok(ApiResponse<DiagramComparisonDto>.Ok(result));
    }

    /// <summary>Validate YAML text or original file bytes without saving a revision.</summary>
    [HttpPost("yaml/decode")]
    [Authorize]
    [ResourceScope<Project>("projectId")]
    [RequirePermission(PermissionCodes.DiagramRead)]
    [ProducesResponseType(typeof(ApiResponse<DiagramModelDto>), StatusCodes.Status200OK)]
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
            var model = DiagramYaml.Read(text);
            await origins.RequireModel(model, null, ct);
            return Ok(ApiResponse<DiagramModelDto>.Ok(new(model)));
        }
        catch (Exception ex) when (ex is InvalidDataException or FormatException or YamlException)
        { throw new ValidationException("yaml", ex.Message); }
    }

    /// <summary>Encode a common model using the canonical YAML codec without saving.</summary>
    [HttpPost("yaml/encode")]
    [Authorize]
    [ResourceScope<Project>("projectId")]
    [RequirePermission(PermissionCodes.DiagramRead)]
    [ProducesResponseType(typeof(ApiResponse<DiagramYamlDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Encode(Guid projectId, [FromBody] DiagramModelDto dto, CancellationToken ct)
    {
        var error = Scope(projectId, out _, out _, out _);
        if (error is not null) return error;
        await origins.RequireModel(dto.Model, null, ct);
        return Ok(ApiResponse<DiagramYamlDto>.Ok(new(DiagramYaml.Write(dto.Model))));
    }

    /// <summary>Preview selected current entities and relationships without saving a revision.</summary>
    [HttpPost("projection")]
    [Authorize]
    [ResourceScope<Project>("projectId")]
    [RequirePermission(PermissionCodes.DiagramRead)]
    [ProducesResponseType(typeof(ApiResponse<DiagramModelDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Project(Guid projectId, [FromBody] DiagramProjectionDto dto, CancellationToken ct)
    {
        var error = Scope(projectId, out var tenant, out var project, out _);
        if (error is not null) return error;
        return Ok(ApiResponse<DiagramModelDto>.Ok(new(await projection.Build(tenant, project, dto, ct))));
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
