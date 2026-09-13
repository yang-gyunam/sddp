using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sddp.Abstractions.Constants;
using Sddp.Abstractions.DTOs;
using Sddp.Abstractions.Enums;
using Sddp.Abstractions.ValueObjects;
using Sddp.Api.Authorization;
using Sddp.Application.Features.Knowledge.Commands;
using Sddp.Application.Features.Knowledge.Queries;
using Sddp.Domain.Entities;

namespace Sddp.Api.Controllers;

/// <summary>
/// Project-scoped editable Knowledge Hub.
/// </summary>
[Route("api/projects/{projectId}/knowledge")]
[Authorize]
public sealed class KnowledgeController : BaseApiController
{
    private readonly ISender _sender;
    private readonly IScopedAuthorizationService _authorizationService;

    public KnowledgeController(
        ISender sender,
        IScopedAuthorizationService authorizationService)
    {
        _sender = sender;
        _authorizationService = authorizationService;
    }

    [HttpGet("tree")]
    [ResourceScope<Project>("projectId")]
    [RequirePermission(PermissionCodes.KnowledgeRead)]
    public async Task<IActionResult> GetTree(
        Guid projectId,
        [FromHeader(Name = "X-Tenant-Id")] string? tenantIdHeader,
        [FromHeader(Name = "X-Project-Id")] string? projectIdHeader,
        [FromQuery] bool includeArchived = false,
        CancellationToken cancellationToken = default)
    {
        var scopeError = RequireKnowledgeScope(
            projectId, tenantIdHeader, projectIdHeader, out var tenantId, out var scopedProjectId);
        if (scopeError is not null) return scopeError;
        var userError = RequireUserId(out var userId, unauthorizedIfMissing: true);
        if (userError is not null) return userError;

        var visibility = await GetVisibilityAsync(
            userId, tenantId, scopedProjectId, includeArchived, cancellationToken);
        var result = await _sender.Send(
            new GetKnowledgeTreeQuery(
                tenantId,
                scopedProjectId,
                userId,
                visibility.CanViewAllDrafts,
                visibility.IncludeArchived),
            cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<KnowledgePageTreeItemDto>>.Ok(result));
    }

    [HttpGet("pages/{pageId}")]
    [ResourceScope<Project>("projectId")]
    [RequirePermission(PermissionCodes.KnowledgeRead)]
    public async Task<IActionResult> GetPage(
        Guid projectId,
        Guid pageId,
        [FromHeader(Name = "X-Tenant-Id")] string? tenantIdHeader,
        [FromHeader(Name = "X-Project-Id")] string? projectIdHeader,
        [FromQuery] bool includeArchived = false,
        CancellationToken cancellationToken = default)
    {
        var scopeError = RequireKnowledgeScope(
            projectId, tenantIdHeader, projectIdHeader, out var tenantId, out var scopedProjectId);
        if (scopeError is not null) return scopeError;
        var userError = RequireUserId(out var userId, unauthorizedIfMissing: true);
        if (userError is not null) return userError;

        var visibility = await GetVisibilityAsync(
            userId, tenantId, scopedProjectId, includeArchived, cancellationToken);
        var result = await _sender.Send(
            new GetKnowledgePageQuery(
                tenantId,
                scopedProjectId,
                GlobalUniqueId.FromGuid(pageId),
                userId,
                visibility.CanViewAllDrafts,
                visibility.IncludeArchived),
            cancellationToken);
        return result is null
            ? NotFoundResponse("Knowledge page")
            : Ok(ApiResponse<KnowledgePageDto>.Ok(result));
    }

    [HttpPost("pages")]
    [ResourceScope<Project>("projectId")]
    [RequirePermission(PermissionCodes.KnowledgeCreate)]
    [Authorize]
    public async Task<IActionResult> CreatePage(
        Guid projectId,
        [FromHeader(Name = "X-Tenant-Id")] string? tenantIdHeader,
        [FromHeader(Name = "X-Project-Id")] string? projectIdHeader,
        [FromBody] CreateKnowledgePageDto dto,
        CancellationToken cancellationToken = default)
    {
        var scopeError = RequireKnowledgeScope(
            projectId, tenantIdHeader, projectIdHeader, out var tenantId, out var scopedProjectId);
        if (scopeError is not null) return scopeError;
        var userError = RequireUserId(out var userId, unauthorizedIfMissing: true);
        if (userError is not null) return userError;

        if (!string.IsNullOrWhiteSpace(dto.ParentId))
        {
            if (!GlobalUniqueId.TryParse(dto.ParentId, out var parentId))
            {
                return ValidationErrorResponse("Parent ID is invalid.");
            }

            var visibility = await GetVisibilityAsync(
                userId, tenantId, scopedProjectId, false, cancellationToken);
            var parent = await _sender.Send(
                new GetKnowledgePageQuery(
                    tenantId,
                    scopedProjectId,
                    parentId,
                    userId,
                    visibility.CanViewAllDrafts,
                    false),
                cancellationToken);
            if (parent is null) return NotFoundResponse("Parent Knowledge page");
        }

        var result = await _sender.Send(
            new CreateKnowledgePageCommand(tenantId, scopedProjectId, userId, dto),
            cancellationToken);
        SetRevisionEtag(result.Revision);
        return Created(
            $"/api/projects/{projectId}/knowledge/pages/{result.Id}",
            ApiResponse<KnowledgePageDto>.Ok(result));
    }

    [HttpPut("pages/{pageId}")]
    [ResourceScope<Project>("projectId")]
    [RequirePermission(PermissionCodes.KnowledgeUpdate)]
    [Authorize]
    public async Task<IActionResult> UpdatePage(
        Guid projectId,
        Guid pageId,
        [FromHeader(Name = "X-Tenant-Id")] string? tenantIdHeader,
        [FromHeader(Name = "X-Project-Id")] string? projectIdHeader,
        [FromHeader(Name = "If-Match")] string? ifMatch,
        [FromBody] UpdateKnowledgePageDto dto,
        CancellationToken cancellationToken = default)
    {
        var context = await PreparePageMutationAsync(
            projectId, pageId, tenantIdHeader, projectIdHeader, ifMatch, false, cancellationToken);
        if (context.Error is not null) return context.Error;

        var result = await _sender.Send(
            new UpdateKnowledgePageCommand(
                context.TenantId,
                context.ProjectId,
                context.PageId,
                context.UserId,
                context.ExpectedRevision,
                dto),
            cancellationToken);
        return RevisionResponse(result);
    }

    [HttpPost("pages/{pageId}/move")]
    [ResourceScope<Project>("projectId")]
    [RequirePermission(PermissionCodes.KnowledgeUpdate)]
    [Authorize]
    public async Task<IActionResult> MovePage(
        Guid projectId,
        Guid pageId,
        [FromHeader(Name = "X-Tenant-Id")] string? tenantIdHeader,
        [FromHeader(Name = "X-Project-Id")] string? projectIdHeader,
        [FromHeader(Name = "If-Match")] string? ifMatch,
        [FromBody] MoveKnowledgePageDto dto,
        CancellationToken cancellationToken = default)
    {
        var context = await PreparePageMutationAsync(
            projectId, pageId, tenantIdHeader, projectIdHeader, ifMatch, false, cancellationToken);
        if (context.Error is not null) return context.Error;

        var result = await _sender.Send(
            new MoveKnowledgePageCommand(
                context.TenantId,
                context.ProjectId,
                context.PageId,
                context.UserId,
                context.ExpectedRevision,
                dto),
            cancellationToken);
        return RevisionResponse(result);
    }

    [HttpPost("pages/{pageId}/publish")]
    [ResourceScope<Project>("projectId")]
    [RequirePermission(PermissionCodes.KnowledgePublish)]
    [Authorize]
    public async Task<IActionResult> PublishPage(
        Guid projectId,
        Guid pageId,
        [FromHeader(Name = "X-Tenant-Id")] string? tenantIdHeader,
        [FromHeader(Name = "X-Project-Id")] string? projectIdHeader,
        [FromHeader(Name = "If-Match")] string? ifMatch,
        CancellationToken cancellationToken = default)
    {
        var context = await PreparePageMutationAsync(
            projectId, pageId, tenantIdHeader, projectIdHeader, ifMatch, false, cancellationToken);
        if (context.Error is not null) return context.Error;

        var result = await _sender.Send(
            new PublishKnowledgePageCommand(
                context.TenantId,
                context.ProjectId,
                context.PageId,
                context.UserId,
                context.ExpectedRevision),
            cancellationToken);
        return RevisionResponse(result);
    }

    [HttpPost("pages/{pageId}/archive")]
    [ResourceScope<Project>("projectId")]
    [RequirePermission(PermissionCodes.KnowledgeArchive)]
    [Authorize]
    public async Task<IActionResult> ArchivePage(
        Guid projectId,
        Guid pageId,
        [FromHeader(Name = "X-Tenant-Id")] string? tenantIdHeader,
        [FromHeader(Name = "X-Project-Id")] string? projectIdHeader,
        [FromHeader(Name = "If-Match")] string? ifMatch,
        [FromBody] ArchiveKnowledgePageDto dto,
        CancellationToken cancellationToken = default)
    {
        var context = await PreparePageMutationAsync(
            projectId, pageId, tenantIdHeader, projectIdHeader, ifMatch, false, cancellationToken);
        if (context.Error is not null) return context.Error;

        var result = await _sender.Send(
            new ArchiveKnowledgePageCommand(
                context.TenantId,
                context.ProjectId,
                context.PageId,
                context.UserId,
                context.ExpectedRevision,
                dto),
            cancellationToken);
        return RevisionResponse(result);
    }

    [HttpPost("pages/{pageId}/restore")]
    [ResourceScope<Project>("projectId")]
    [RequirePermission(PermissionCodes.KnowledgeArchive)]
    [Authorize]
    public async Task<IActionResult> RestorePage(
        Guid projectId,
        Guid pageId,
        [FromHeader(Name = "X-Tenant-Id")] string? tenantIdHeader,
        [FromHeader(Name = "X-Project-Id")] string? projectIdHeader,
        [FromHeader(Name = "If-Match")] string? ifMatch,
        CancellationToken cancellationToken = default)
    {
        var context = await PreparePageMutationAsync(
            projectId, pageId, tenantIdHeader, projectIdHeader, ifMatch, true, cancellationToken);
        if (context.Error is not null) return context.Error;

        var result = await _sender.Send(
            new RestoreKnowledgePageCommand(
                context.TenantId,
                context.ProjectId,
                context.PageId,
                context.UserId,
                context.ExpectedRevision),
            cancellationToken);
        return RevisionResponse(result);
    }

    [HttpGet("pages/{pageId}/versions")]
    [ResourceScope<Project>("projectId")]
    [RequirePermission(PermissionCodes.KnowledgeRead)]
    public async Task<IActionResult> GetVersions(
        Guid projectId,
        Guid pageId,
        [FromHeader(Name = "X-Tenant-Id")] string? tenantIdHeader,
        [FromHeader(Name = "X-Project-Id")] string? projectIdHeader,
        [FromQuery] bool includeArchived = false,
        CancellationToken cancellationToken = default)
    {
        var scopeError = RequireKnowledgeScope(
            projectId, tenantIdHeader, projectIdHeader, out var tenantId, out var scopedProjectId);
        if (scopeError is not null) return scopeError;
        var userError = RequireUserId(out var userId, unauthorizedIfMissing: true);
        if (userError is not null) return userError;
        var visibility = await GetVisibilityAsync(
            userId, tenantId, scopedProjectId, includeArchived, cancellationToken);
        var result = await _sender.Send(
            new GetKnowledgePageVersionsQuery(
                tenantId,
                scopedProjectId,
                GlobalUniqueId.FromGuid(pageId),
                userId,
                visibility.CanViewAllDrafts,
                visibility.IncludeArchived),
            cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<KnowledgePageVersionSummaryDto>>.Ok(result));
    }

    [HttpGet("pages/{pageId}/versions/{revision:int}")]
    [ResourceScope<Project>("projectId")]
    [RequirePermission(PermissionCodes.KnowledgeRead)]
    public async Task<IActionResult> GetVersion(
        Guid projectId,
        Guid pageId,
        int revision,
        [FromHeader(Name = "X-Tenant-Id")] string? tenantIdHeader,
        [FromHeader(Name = "X-Project-Id")] string? projectIdHeader,
        [FromQuery] bool includeArchived = false,
        CancellationToken cancellationToken = default)
    {
        var scopeError = RequireKnowledgeScope(
            projectId, tenantIdHeader, projectIdHeader, out var tenantId, out var scopedProjectId);
        if (scopeError is not null) return scopeError;
        var userError = RequireUserId(out var userId, unauthorizedIfMissing: true);
        if (userError is not null) return userError;
        var visibility = await GetVisibilityAsync(
            userId, tenantId, scopedProjectId, includeArchived, cancellationToken);
        var result = await _sender.Send(
            new GetKnowledgePageVersionQuery(
                tenantId,
                scopedProjectId,
                GlobalUniqueId.FromGuid(pageId),
                revision,
                userId,
                visibility.CanViewAllDrafts,
                visibility.IncludeArchived),
            cancellationToken);
        return result is null
            ? NotFoundResponse("Knowledge page version")
            : Ok(ApiResponse<KnowledgePageVersionDto>.Ok(result));
    }

    [HttpPost("pages/{pageId}/versions/{revision:int}/restore")]
    [ResourceScope<Project>("projectId")]
    [RequirePermission(PermissionCodes.KnowledgeUpdate)]
    [Authorize]
    public async Task<IActionResult> RestoreVersion(
        Guid projectId,
        Guid pageId,
        int revision,
        [FromHeader(Name = "X-Tenant-Id")] string? tenantIdHeader,
        [FromHeader(Name = "X-Project-Id")] string? projectIdHeader,
        [FromHeader(Name = "If-Match")] string? ifMatch,
        CancellationToken cancellationToken = default)
    {
        var context = await PreparePageMutationAsync(
            projectId, pageId, tenantIdHeader, projectIdHeader, ifMatch, false, cancellationToken);
        if (context.Error is not null) return context.Error;

        var result = await _sender.Send(
            new RestoreKnowledgePageVersionCommand(
                context.TenantId,
                context.ProjectId,
                context.PageId,
                revision,
                context.UserId,
                context.ExpectedRevision),
            cancellationToken);
        return RevisionResponse(result);
    }

    [HttpGet("search")]
    [ResourceScope<Project>("projectId")]
    [RequirePermission(PermissionCodes.KnowledgeRead)]
    public async Task<IActionResult> Search(
        Guid projectId,
        [FromHeader(Name = "X-Tenant-Id")] string? tenantIdHeader,
        [FromHeader(Name = "X-Project-Id")] string? projectIdHeader,
        [FromQuery] string q = "",
        [FromQuery] int limit = 20,
        [FromQuery] bool includeArchived = false,
        CancellationToken cancellationToken = default)
    {
        var scopeError = RequireKnowledgeScope(
            projectId, tenantIdHeader, projectIdHeader, out var tenantId, out var scopedProjectId);
        if (scopeError is not null) return scopeError;
        var userError = RequireUserId(out var userId, unauthorizedIfMissing: true);
        if (userError is not null) return userError;
        var visibility = await GetVisibilityAsync(
            userId, tenantId, scopedProjectId, includeArchived, cancellationToken);
        var result = await _sender.Send(
            new SearchKnowledgePagesQuery(
                tenantId,
                scopedProjectId,
                userId,
                q,
                limit,
                visibility.CanViewAllDrafts,
                visibility.IncludeArchived),
            cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<KnowledgeSearchResultDto>>.Ok(result));
    }

    [HttpGet("backlinks/{targetType}/{targetId}")]
    [ResourceScope<Project>("projectId")]
    [RequirePermission(PermissionCodes.KnowledgeRead)]
    public async Task<IActionResult> GetBacklinks(
        Guid projectId,
        KnowledgeTargetType targetType,
        Guid targetId,
        [FromHeader(Name = "X-Tenant-Id")] string? tenantIdHeader,
        [FromHeader(Name = "X-Project-Id")] string? projectIdHeader,
        [FromQuery] bool includeArchived = false,
        CancellationToken cancellationToken = default)
    {
        var scopeError = RequireKnowledgeScope(
            projectId, tenantIdHeader, projectIdHeader, out var tenantId, out var scopedProjectId);
        if (scopeError is not null) return scopeError;
        var userError = RequireUserId(out var userId, unauthorizedIfMissing: true);
        if (userError is not null) return userError;
        var targetPermission = targetType switch
        {
            KnowledgeTargetType.Spec => PermissionCodes.SpecRead,
            KnowledgeTargetType.Requirement => PermissionCodes.RequirementRead,
            KnowledgeTargetType.Decision => PermissionCodes.ConversationRead,
            KnowledgeTargetType.Artifact => PermissionCodes.ProjectRead,
            _ => PermissionCodes.ProjectRead
        };
        if (!await _authorizationService.HasPermissionAsync(
                userId,
                tenantId,
                scopedProjectId,
                targetPermission,
                cancellationToken))
        {
            return StatusCode(
                StatusCodes.Status403Forbidden,
                ApiResponse<object>.Fail(
                    "FORBIDDEN",
                    "Read access to the referenced entity is required."));
        }
        var visibility = await GetVisibilityAsync(
            userId, tenantId, scopedProjectId, includeArchived, cancellationToken);
        var result = await _sender.Send(
            new GetKnowledgeBacklinksQuery(
                tenantId,
                scopedProjectId,
                userId,
                targetType,
                GlobalUniqueId.FromGuid(targetId),
                visibility.CanViewAllDrafts,
                visibility.IncludeArchived),
            cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<KnowledgeBacklinkDto>>.Ok(result));
    }

    private IActionResult? RequireKnowledgeScope(
        Guid pathProjectId,
        string? tenantIdHeader,
        string? projectIdHeader,
        out GlobalUniqueId tenantId,
        out GlobalUniqueId projectId)
    {
        var scopeError = RequireTenantAndProject(
            tenantIdHeader,
            projectIdHeader,
            out tenantId,
            out projectId,
            "INVALID_KNOWLEDGE_SCOPE",
            "Valid X-Tenant-Id and X-Project-Id headers are required.");
        if (scopeError is not null) return scopeError;

        return projectId == GlobalUniqueId.FromGuid(pathProjectId)
            ? null
            : BadRequest(ApiResponse<object>.Fail(
                "PROJECT_SCOPE_MISMATCH",
                "Route projectId must match X-Project-Id."));
    }

    private async Task<(bool CanViewAllDrafts, bool IncludeArchived)> GetVisibilityAsync(
        GlobalUniqueId userId,
        GlobalUniqueId tenantId,
        GlobalUniqueId projectId,
        bool includeArchived,
        CancellationToken cancellationToken)
    {
        var canPublish = await _authorizationService.HasPermissionAsync(
            userId,
            tenantId,
            projectId,
            PermissionCodes.KnowledgePublish,
            cancellationToken);
        var isAdmin = await _authorizationService.HasPermissionAsync(
            userId,
            tenantId,
            projectId,
            PermissionCodes.KnowledgeAdmin,
            cancellationToken);
        return (canPublish || isAdmin, includeArchived && isAdmin);
    }

    private async Task<PageMutationContext> PreparePageMutationAsync(
        Guid pathProjectId,
        Guid pathPageId,
        string? tenantIdHeader,
        string? projectIdHeader,
        string? ifMatch,
        bool includeArchived,
        CancellationToken cancellationToken)
    {
        var scopeError = RequireKnowledgeScope(
            pathProjectId,
            tenantIdHeader,
            projectIdHeader,
            out var tenantId,
            out var projectId);
        if (scopeError is not null) return PageMutationContext.Failed(scopeError);

        var userError = RequireUserId(out var userId, unauthorizedIfMissing: true);
        if (userError is not null) return PageMutationContext.Failed(userError);

        if (!TryParseRevision(ifMatch, out var expectedRevision))
        {
            return PageMutationContext.Failed(StatusCode(
                StatusCodes.Status428PreconditionRequired,
                ApiResponse<object>.Fail(
                    "PRECONDITION_REQUIRED",
                    "A valid If-Match page revision is required.")));
        }

        var visibility = await GetVisibilityAsync(
            userId, tenantId, projectId, includeArchived, cancellationToken);
        var pageId = GlobalUniqueId.FromGuid(pathPageId);
        var page = await _sender.Send(
            new GetKnowledgePageQuery(
                tenantId,
                projectId,
                pageId,
                userId,
                visibility.CanViewAllDrafts,
                visibility.IncludeArchived),
            cancellationToken);
        if (page is null) return PageMutationContext.Failed(NotFoundResponse("Knowledge page"));

        return new PageMutationContext(
            tenantId,
            projectId,
            pageId,
            userId,
            expectedRevision,
            null);
    }

    private IActionResult RevisionResponse(KnowledgePageDto page)
    {
        SetRevisionEtag(page.Revision);
        return Ok(ApiResponse<KnowledgePageDto>.Ok(page));
    }

    private void SetRevisionEtag(int revision)
    {
        Response.Headers.ETag = $"\"{revision}\"";
    }

    private static bool TryParseRevision(string? ifMatch, out int revision)
    {
        var value = ifMatch?.Trim();
        if (value?.StartsWith("W/", StringComparison.OrdinalIgnoreCase) == true)
        {
            value = value[2..].Trim();
        }

        value = value?.Trim('"');
        return int.TryParse(value, out revision) && revision > 0;
    }

    private sealed record PageMutationContext(
        GlobalUniqueId TenantId,
        GlobalUniqueId ProjectId,
        GlobalUniqueId PageId,
        GlobalUniqueId UserId,
        int ExpectedRevision,
        IActionResult? Error)
    {
        public static PageMutationContext Failed(IActionResult error) =>
            new(default, default, default, default, 0, error);
    }
}
