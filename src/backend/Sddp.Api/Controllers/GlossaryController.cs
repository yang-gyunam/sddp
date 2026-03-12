using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sddp.Abstractions.DTOs;
using Sddp.Abstractions.Enums;
using Sddp.Abstractions.ValueObjects;
using Sddp.Api.Constants;
using Sddp.Application.Features.AuditLogs.Queries;
using Sddp.Application.Features.Glossary.Commands;
using Sddp.Application.Features.Glossary.Queries;

namespace Sddp.Api.Controllers;

#region Request DTOs

/// <summary>
/// DTO
/// </summary>
public record DetectConflictRequestDto(
    string Term,
    string? Definition = null,
    string? ExcludeTermId = null);

/// <summary>
/// glossary DTO
/// </summary>
public record AddRelatedTermDto(
    string RelatedTermId);

/// <summary>
/// DTO
/// </summary>
public record AddUsageExampleDto(
    string Example);

#endregion

/// <summary>
/// glossary controller
/// </summary>
[Route("api/glossary")]
[Authorize]
public class GlossaryController : BaseApiController
{
    private readonly ISender _sender;

    public GlossaryController(ISender sender)
    {
        _sender = sender;
    }

    [HttpGet]
    public async Task<IActionResult> GetTerms(
        [FromHeader(Name = "X-Tenant-Id")] string? tenantIdHeader,
        [FromHeader(Name = "X-Project-Id")] string? projectIdHeader,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] TermCategory? category = null,
        [FromQuery] GlossaryTermStatus? status = null,
        CancellationToken cancellationToken = default)
    {
        var tenantError = RequireTenantId(tenantIdHeader, out var tenantId);
        if (tenantError is not null)
        {
            return tenantError;
        }

        var projectError = TryGetProjectId(projectIdHeader, out var projectId);
        if (projectError is not null)
        {
            return projectError;
        }

        var result = await _sender.Send(new GetGlossaryTermsQuery(
            tenantId,
            projectId,
            page,
            pageSize,
            category,
            status), cancellationToken);

        return Ok(ApiResponse<GlossaryTermPageDto>.Ok(result));
    }

    [HttpGet("search")]
    public async Task<IActionResult> SearchTerms(
        [FromHeader(Name = "X-Tenant-Id")] string? tenantIdHeader,
        [FromHeader(Name = "X-Project-Id")] string? projectIdHeader,
        [FromQuery] string q = "",
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] TermCategory? category = null,
        [FromQuery] GlossaryTermStatus? status = null,
        CancellationToken cancellationToken = default)
    {
        var tenantError = RequireTenantId(tenantIdHeader, out var tenantId);
        if (tenantError is not null)
        {
            return tenantError;
        }

        var projectError = RequireProjectId(projectIdHeader, out var projectId);
        if (projectError is not null)
        {
            return projectError;
        }

        if (string.IsNullOrWhiteSpace(q))
        {
            return BadRequest(ApiResponse<object>.Fail(ApiErrorCodes.Validation.InvalidQuery, "Search query is required"));
        }

        var result = await _sender.Send(new SearchGlossaryTermsQuery(
            tenantId,
            projectId,
            q,
            page,
            pageSize,
            category,
            status), cancellationToken);

        return Ok(ApiResponse<GlossaryTermPageDto>.Ok(result));
    }

    [HttpGet("autocomplete")]
    public async Task<IActionResult> Autocomplete(
        [FromHeader(Name = "X-Tenant-Id")] string? tenantIdHeader,
        [FromHeader(Name = "X-Project-Id")] string? projectIdHeader,
        [FromQuery] string prefix = "",
        [FromQuery] int limit = 10,
        CancellationToken cancellationToken = default)
    {
        var tenantError = RequireTenantId(tenantIdHeader, out var tenantId);
        if (tenantError is not null)
        {
            return tenantError;
        }

        var projectError = RequireProjectId(projectIdHeader, out var projectId);
        if (projectError is not null)
        {
            return projectError;
        }

        if (string.IsNullOrWhiteSpace(prefix))
        {
            return Ok(ApiResponse<List<GlossaryTermSummaryDto>>.Ok([]));
        }

        var result = await _sender.Send(new AutocompleteGlossaryTermsQuery(
            tenantId,
            projectId,
            prefix,
            limit), cancellationToken);

        return Ok(ApiResponse<List<GlossaryTermSummaryDto>>.Ok(result));
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetTermById(
        Guid id,
        [FromHeader(Name = "X-Tenant-Id")] string? tenantIdHeader,
        [FromHeader(Name = "X-Project-Id")] string? projectIdHeader,
        CancellationToken cancellationToken = default)
    {
        var tenantError = RequireTenantId(tenantIdHeader, out var tenantId);
        if (tenantError is not null)
        {
            return tenantError;
        }

        var projectError = RequireProjectId(projectIdHeader, out var projectId);
        if (projectError is not null)
        {
            return projectError;
        }

        var termId = GlobalUniqueId.FromGuid(id);
        var result = await _sender.Send(new GetGlossaryTermByIdQuery(tenantId, projectId, termId), cancellationToken);

        if (result is null)
        {
            return NotFound(ApiResponse<object>.Fail(ApiErrorCodes.Crud.NotFound, "Term not found"));
        }

        return Ok(ApiResponse<GlossaryTermDetailDto>.Ok(result));
    }

    [HttpGet("term/{term}")]
    public async Task<IActionResult> GetTermByTerm(
        string term,
        [FromHeader(Name = "X-Tenant-Id")] string? tenantIdHeader,
        [FromHeader(Name = "X-Project-Id")] string? projectIdHeader,
        CancellationToken cancellationToken = default)
    {
        var tenantError = RequireTenantId(tenantIdHeader, out var tenantId);
        if (tenantError is not null)
        {
            return tenantError;
        }

        var projectError = RequireProjectId(projectIdHeader, out var projectId);
        if (projectError is not null)
        {
            return projectError;
        }

        var result = await _sender.Send(new GetGlossaryTermByTermQuery(tenantId, projectId, term), cancellationToken);

        if (result is null)
        {
            return NotFound(ApiResponse<object>.Fail(ApiErrorCodes.Crud.NotFound, "Term not found"));
        }

        return Ok(ApiResponse<GlossaryTermDetailDto>.Ok(result));
    }

    [HttpPost]
    [Authorize]
    public async Task<IActionResult> CreateTerm(
        [FromHeader(Name = "X-Tenant-Id")] string? tenantIdHeader,
        [FromHeader(Name = "X-Project-Id")] string? projectIdHeader,
        [FromBody] CreateGlossaryTermDto dto,
        CancellationToken cancellationToken = default)
    {
        var tenantError = RequireTenantId(tenantIdHeader, out var tenantId);
        if (tenantError is not null)
        {
            return tenantError;
        }

        var projectError = RequireProjectId(projectIdHeader, out var projectId);
        if (projectError is not null)
        {
            return projectError;
        }

        var userError = RequireUserId(out var userId);
        if (userError is not null)
        {
            return userError;
        }

        var result = await _sender.Send(new CreateGlossaryTermCommand(tenantId, projectId, userId, dto), cancellationToken);

        if (result is null)
        {
            return BadRequest(ApiResponse<object>.Fail(ApiErrorCodes.Crud.CreateFailed, "Failed to create term"));
        }

        return Created($"/api/glossary/{result.Id}", ApiResponse<GlossaryTermDetailDto>.Ok(result));
    }

    [HttpPut("{id:guid}")]
    [Authorize]
    public async Task<IActionResult> UpdateTerm(
        Guid id,
        [FromHeader(Name = "X-Tenant-Id")] string? tenantIdHeader,
        [FromHeader(Name = "X-Project-Id")] string? projectIdHeader,
        [FromBody] UpdateGlossaryTermDto dto,
        CancellationToken cancellationToken = default)
    {
        var tenantError = RequireTenantId(tenantIdHeader, out var tenantId);
        if (tenantError is not null)
        {
            return tenantError;
        }

        var projectError = RequireProjectId(projectIdHeader, out var projectId);
        if (projectError is not null)
        {
            return projectError;
        }

        var userError = RequireUserId(out var userId);
        if (userError is not null)
        {
            return userError;
        }

        var termId = GlobalUniqueId.FromGuid(id);

        var result = await _sender.Send(new UpdateGlossaryTermCommand(tenantId, projectId, termId, userId, dto), cancellationToken);

        if (result is null)
        {
            return NotFound(ApiResponse<object>.Fail(ApiErrorCodes.Crud.NotFound, "Term not found"));
        }

        return Ok(ApiResponse<GlossaryTermDetailDto>.Ok(result));
    }

    [HttpPost("{id:guid}/approve")]
    [Authorize]
    public async Task<IActionResult> ApproveTerm(
        Guid id,
        [FromHeader(Name = "X-Tenant-Id")] string? tenantIdHeader,
        [FromHeader(Name = "X-Project-Id")] string? projectIdHeader,
        CancellationToken cancellationToken = default)
    {
        var tenantError = RequireTenantId(tenantIdHeader, out var tenantId);
        if (tenantError is not null)
        {
            return tenantError;
        }

        var projectError = RequireProjectId(projectIdHeader, out var projectId);
        if (projectError is not null)
        {
            return projectError;
        }

        var userError = RequireUserId(out var userId);
        if (userError is not null)
        {
            return userError;
        }

        var termId = GlobalUniqueId.FromGuid(id);

        var result = await _sender.Send(new ApproveGlossaryTermCommand(tenantId, projectId, termId, userId), cancellationToken);

        if (result is null)
        {
            return NotFound(ApiResponse<object>.Fail(ApiErrorCodes.Crud.NotFound, "Term not found"));
        }

        return Ok(ApiResponse<GlossaryTermDto>.Ok(result));
    }

    [HttpPost("{id:guid}/deprecate")]
    [Authorize]
    public async Task<IActionResult> DeprecateTerm(
        Guid id,
        [FromHeader(Name = "X-Tenant-Id")] string? tenantIdHeader,
        [FromHeader(Name = "X-Project-Id")] string? projectIdHeader,
        [FromBody] DeprecateGlossaryTermDto? dto,
        CancellationToken cancellationToken = default)
    {
        var tenantError = RequireTenantId(tenantIdHeader, out var tenantId);
        if (tenantError is not null)
        {
            return tenantError;
        }

        var projectError = RequireProjectId(projectIdHeader, out var projectId);
        if (projectError is not null)
        {
            return projectError;
        }

        var userError = RequireUserId(out var userId);
        if (userError is not null)
        {
            return userError;
        }

        var termId = GlobalUniqueId.FromGuid(id);

        GlobalUniqueId? replacementTermId = null;
        if (!string.IsNullOrEmpty(dto?.ReplacementTermId))
        {
            if (!GlobalUniqueId.TryParse(dto.ReplacementTermId, out var parsedReplacementId))
            {
                return BadRequest(ApiResponse<object>.Fail(ApiErrorCodes.InvalidEntity.ReplacementId, "Invalid replacement term ID"));
            }
            replacementTermId = parsedReplacementId;
        }

        var result = await _sender.Send(new DeprecateGlossaryTermCommand(
            tenantId,
            projectId,
            termId,
            userId,
            replacementTermId), cancellationToken);

        if (result is null)
        {
            return NotFound(ApiResponse<object>.Fail(ApiErrorCodes.Crud.NotFound, "Term not found"));
        }

        return Ok(ApiResponse<GlossaryTermDto>.Ok(result));
    }

    [HttpPost("{id:guid}/reactivate")]
    [Authorize]
    public async Task<IActionResult> ReactivateTerm(
        Guid id,
        [FromHeader(Name = "X-Tenant-Id")] string? tenantIdHeader,
        [FromHeader(Name = "X-Project-Id")] string? projectIdHeader,
        CancellationToken cancellationToken = default)
    {
        var tenantError = RequireTenantId(tenantIdHeader, out var tenantId);
        if (tenantError is not null)
        {
            return tenantError;
        }

        var projectError = RequireProjectId(projectIdHeader, out var projectId);
        if (projectError is not null)
        {
            return projectError;
        }

        var userError = RequireUserId(out var userId);
        if (userError is not null)
        {
            return userError;
        }

        var termId = GlobalUniqueId.FromGuid(id);

        var result = await _sender.Send(new ReactivateGlossaryTermCommand(
            tenantId,
            projectId,
            termId,
            userId), cancellationToken);

        if (result is null)
        {
            return NotFound(ApiResponse<object>.Fail(ApiErrorCodes.Crud.NotFound, "Term not found"));
        }

        return Ok(ApiResponse<GlossaryTermDto>.Ok(result));
    }

    [HttpDelete("{id:guid}")]
    [Authorize]
    public async Task<IActionResult> DeleteTerm(
        Guid id,
        [FromHeader(Name = "X-Tenant-Id")] string? tenantIdHeader,
        [FromHeader(Name = "X-Project-Id")] string? projectIdHeader,
        CancellationToken cancellationToken = default)
    {
        var tenantError = RequireTenantId(tenantIdHeader, out var tenantId);
        if (tenantError is not null)
        {
            return tenantError;
        }

        var projectError = RequireProjectId(projectIdHeader, out var projectId);
        if (projectError is not null)
        {
            return projectError;
        }

        var termId = GlobalUniqueId.FromGuid(id);
        var result = await _sender.Send(new DeleteGlossaryTermCommand(tenantId, projectId, termId), cancellationToken);

        if (!result)
        {
            return NotFound(ApiResponse<object>.Fail(ApiErrorCodes.Crud.NotFound, "Term not found"));
        }

        return Ok(ApiResponse.Ok());
    }

    [HttpPost("detect-conflicts")]
    [Authorize]
    public async Task<IActionResult> DetectConflicts(
        [FromHeader(Name = "X-Tenant-Id")] string? tenantIdHeader,
        [FromHeader(Name = "X-Project-Id")] string? projectIdHeader,
        [FromBody] DetectConflictRequestDto dto,
        CancellationToken cancellationToken = default)
    {
        var tenantError = RequireTenantId(tenantIdHeader, out var tenantId);
        if (tenantError is not null)
        {
            return tenantError;
        }

        var projectError = RequireProjectId(projectIdHeader, out var projectId);
        if (projectError is not null)
        {
            return projectError;
        }

        if (string.IsNullOrWhiteSpace(dto.Term))
        {
            return BadRequest(ApiResponse<object>.Fail(ApiErrorCodes.InvalidEntity.Term, "Term is required"));
        }

        GlobalUniqueId? excludeTermId = null;
        if (!string.IsNullOrEmpty(dto.ExcludeTermId))
        {
            if (!GlobalUniqueId.TryParse(dto.ExcludeTermId, out var parsedExcludeId))
            {
                return BadRequest(ApiResponse<object>.Fail(ApiErrorCodes.InvalidEntity.ExcludeId, "Invalid exclude term ID"));
            }
            excludeTermId = parsedExcludeId;
        }

        var result = await _sender.Send(new DetectGlossaryConflictsQuery(
            tenantId,
            projectId,
            dto.Term,
            dto.Definition,
            excludeTermId), cancellationToken);

        return Ok(ApiResponse<GlossaryConflictResultDto>.Ok(result));
    }

    [HttpGet("{id:guid}/usage")]
    public async Task<IActionResult> GetTermUsage(
        Guid id,
        [FromHeader(Name = "X-Tenant-Id")] string? tenantIdHeader,
        [FromHeader(Name = "X-Project-Id")] string? projectIdHeader,
        CancellationToken cancellationToken = default)
    {
        var tenantError = RequireTenantId(tenantIdHeader, out var tenantId);
        if (tenantError is not null)
        {
            return tenantError;
        }

        var projectError = RequireProjectId(projectIdHeader, out var projectId);
        if (projectError is not null)
        {
            return projectError;
        }

        var termId = GlobalUniqueId.FromGuid(id);
        var result = await _sender.Send(new GetGlossaryTermUsageQuery(tenantId, projectId, termId), cancellationToken);

        return Ok(ApiResponse<GlossaryTermUsageDto>.Ok(result));
    }

    [HttpGet("{id:guid}/versions")]
    public async Task<IActionResult> GetVersionHistory(
        Guid id,
        [FromHeader(Name = "X-Tenant-Id")] string? tenantIdHeader,
        [FromHeader(Name = "X-Project-Id")] string? projectIdHeader,
        CancellationToken cancellationToken = default)
    {
        var tenantError = RequireTenantId(tenantIdHeader, out var tenantId);
        if (tenantError is not null)
        {
            return tenantError;
        }

        var projectError = RequireProjectId(projectIdHeader, out var projectId);
        if (projectError is not null)
        {
            return projectError;
        }

        var termId = GlobalUniqueId.FromGuid(id);
        var result = await _sender.Send(new GetGlossaryTermVersionHistoryQuery(tenantId, projectId, termId), cancellationToken);

        return Ok(ApiResponse<List<GlossaryTermVersionDto>>.Ok(result));
    }

    [HttpPost("{id:guid}/related-terms")]
    [Authorize]
    public async Task<IActionResult> AddRelatedTerm(
        Guid id,
        [FromHeader(Name = "X-Tenant-Id")] string? tenantIdHeader,
        [FromHeader(Name = "X-Project-Id")] string? projectIdHeader,
        [FromBody] AddRelatedTermDto dto,
        CancellationToken cancellationToken = default)
    {
        var tenantError = RequireTenantId(tenantIdHeader, out var tenantId);
        if (tenantError is not null)
        {
            return tenantError;
        }

        var projectError = RequireProjectId(projectIdHeader, out var projectId);
        if (projectError is not null)
        {
            return projectError;
        }

        if (!GlobalUniqueId.TryParse(dto.RelatedTermId, out var relatedTermId))
        {
            return BadRequest(ApiResponse<object>.Fail(ApiErrorCodes.InvalidEntity.RelatedId, "Invalid related term ID"));
        }

        var termId = GlobalUniqueId.FromGuid(id);

        var result = await _sender.Send(new AddGlossaryRelatedTermCommand(
            tenantId,
            projectId,
            termId,
            relatedTermId), cancellationToken);

        if (result is null)
        {
            return NotFound(ApiResponse<object>.Fail(ApiErrorCodes.Crud.NotFound, "Term not found"));
        }

        return Ok(ApiResponse<GlossaryTermDto>.Ok(result));
    }

    [HttpPost("{id:guid}/examples")]
    [Authorize]
    public async Task<IActionResult> AddUsageExample(
        Guid id,
        [FromHeader(Name = "X-Tenant-Id")] string? tenantIdHeader,
        [FromHeader(Name = "X-Project-Id")] string? projectIdHeader,
        [FromBody] AddUsageExampleDto dto,
        CancellationToken cancellationToken = default)
    {
        var tenantError = RequireTenantId(tenantIdHeader, out var tenantId);
        if (tenantError is not null)
        {
            return tenantError;
        }

        var projectError = RequireProjectId(projectIdHeader, out var projectId);
        if (projectError is not null)
        {
            return projectError;
        }

        if (string.IsNullOrWhiteSpace(dto.Example))
        {
            return BadRequest(ApiResponse<object>.Fail(ApiErrorCodes.InvalidEntity.Example, "Example is required"));
        }

        var termId = GlobalUniqueId.FromGuid(id);

        var result = await _sender.Send(new AddGlossaryUsageExampleCommand(
            tenantId,
            projectId,
            termId,
            dto.Example), cancellationToken);

        if (result is null)
        {
            return NotFound(ApiResponse<object>.Fail(ApiErrorCodes.Crud.NotFound, "Term not found"));
        }

        return Ok(ApiResponse<GlossaryTermDto>.Ok(result));
    }

    /// <summary>
    /// glossary update get
    /// </summary>
    [HttpGet("{id:guid}/field-authors")]
    public async Task<IActionResult> GetFieldAuthors(
        Guid id,
        [FromHeader(Name = "X-Tenant-Id")] string? tenantIdHeader,
        [FromHeader(Name = "X-Project-Id")] string? projectIdHeader,
        CancellationToken cancellationToken = default)
    {
        var tenantError = RequireTenantId(tenantIdHeader, out var tenantId);
        if (tenantError is not null)
        {
            return tenantError;
        }

        var projectError = RequireProjectId(projectIdHeader, out var projectId);
        if (projectError is not null)
        {
            return projectError;
        }

        var termId = GlobalUniqueId.FromGuid(id);

        var term = await _sender.Send(new GetGlossaryTermByIdQuery(tenantId, projectId, termId), cancellationToken);
        if (term is null)
        {
            return NotFound(ApiResponse<object>.Fail(ApiErrorCodes.Crud.NotFound, "Term not found"));
        }

        var result = await _sender.Send(
            new GetFieldAuthorsQuery("glossary_term", termId), cancellationToken);
        return Ok(ApiResponse<List<FieldAuthorDto>>.Ok(result));
    }
}
