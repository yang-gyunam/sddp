using MediatR;
using Sddp.Abstractions.DTOs;
using Sddp.Abstractions.Exceptions;
using Sddp.Abstractions.Interfaces;
using Sddp.Abstractions.ValueObjects;
using Sddp.Application.Requests;
using Sddp.Domain.Entities;

namespace Sddp.Application.Features.Knowledge.Commands;

public sealed record CreateKnowledgePageCommand(
    GlobalUniqueId TenantId,
    GlobalUniqueId ProjectId,
    GlobalUniqueId UserId,
    CreateKnowledgePageDto Dto)
    : ICommand<KnowledgePageDto>, IAuditableRequest<KnowledgePageDto>
{
    public AuditLogRequest? AuditLog { get; set; }
    public AuditLogRequest? GetAuditLogRequest(KnowledgePageDto response) => AuditLog;
}

public sealed class CreateKnowledgePageCommandHandler
    : IRequestHandler<CreateKnowledgePageCommand, KnowledgePageDto>
{
    private readonly IUnitOfWork _unitOfWork;

    public CreateKnowledgePageCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<KnowledgePageDto> Handle(
        CreateKnowledgePageCommand request,
        CancellationToken cancellationToken)
    {
        await KnowledgeCommandHelpers.EnsureProjectAsync(
            _unitOfWork,
            request.TenantId,
            request.ProjectId,
            cancellationToken);

        GlobalUniqueId? parentId = null;
        KnowledgePage? parentPage = null;
        if (!string.IsNullOrWhiteSpace(request.Dto.ParentId))
        {
            if (!GlobalUniqueId.TryParse(request.Dto.ParentId, out var parsedParentId))
            {
                throw new ValidationException("parentId", "Parent ID is invalid.");
            }

            parentPage = await KnowledgeCommandHelpers.GetParentAsync(
                _unitOfWork,
                request.TenantId,
                request.ProjectId,
                parsedParentId,
                cancellationToken);
            parentId = parsedParentId;
        }

        if (parentPage is not null)
        {
            var projectPages = await _unitOfWork.Repository<KnowledgePage>().FindAsync(
                page => page.TenantId == request.TenantId
                    && page.ProjectId == request.ProjectId
                    && page.IsActive,
                cancellationToken);
            var pagesById = projectPages.ToDictionary(page => page.Id);
            var depth = 2;
            var cursor = parentPage.ParentId;
            while (cursor.HasValue)
            {
                depth++;
                cursor = pagesById.TryGetValue(cursor.Value, out var ancestor)
                    ? ancestor.ParentId
                    : null;
            }

            if (depth > 10)
            {
                throw new ValidationException(
                    "parentId",
                    "Knowledge Page depth cannot exceed 10.");
            }
        }

        var title = KnowledgeCommandHelpers.NormalizeTitle(request.Dto.Title);
        var slug = await KnowledgeCommandHelpers.CreateAvailableSlugAsync(
            _unitOfWork,
            request.TenantId,
            request.ProjectId,
            parentId,
            title,
            cancellationToken);
        var siblings = await _unitOfWork.Repository<KnowledgePage>().FindAsync(
            page => page.TenantId == request.TenantId
                && page.ProjectId == request.ProjectId
                && page.ParentId == parentId,
            cancellationToken);
        var position = siblings.Count == 0
            ? 10
            : siblings.Max(page => page.Position) + 10;
        var content = KnowledgeContentParser.Parse(
            KnowledgeTemplates.Create(request.Dto.Template));

        var page = new KnowledgePage(
            request.TenantId,
            request.ProjectId,
            parentId,
            title,
            slug,
            content.Json,
            content.PlainText,
            "[]",
            position,
            request.UserId);
        await _unitOfWork.Repository<KnowledgePage>().AddAsync(page, cancellationToken);
        await _unitOfWork.Repository<KnowledgePageVersion>().AddAsync(
            new KnowledgePageVersion(page, request.UserId, "Initial draft"),
            cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        request.AuditLog = new AuditLogRequest(
            request.UserId,
            "create",
            "knowledge_page",
            page.Id,
            new { page.Title, page.ParentId, page.Revision },
            request.TenantId,
            request.ProjectId);

        return KnowledgeMapping.ToDto(page);
    }
}

public sealed record UpdateKnowledgePageCommand(
    GlobalUniqueId TenantId,
    GlobalUniqueId ProjectId,
    GlobalUniqueId PageId,
    GlobalUniqueId UserId,
    int ExpectedRevision,
    UpdateKnowledgePageDto Dto)
    : ICommand<KnowledgePageDto>, IAuditableRequest<KnowledgePageDto>
{
    public AuditLogRequest? AuditLog { get; set; }
    public AuditLogRequest? GetAuditLogRequest(KnowledgePageDto response) => AuditLog;
}

public sealed class UpdateKnowledgePageCommandHandler
    : IRequestHandler<UpdateKnowledgePageCommand, KnowledgePageDto>
{
    private readonly IUnitOfWork _unitOfWork;

    public UpdateKnowledgePageCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<KnowledgePageDto> Handle(
        UpdateKnowledgePageCommand request,
        CancellationToken cancellationToken)
    {
        var page = await KnowledgeCommandHelpers.GetPageAsync(
            _unitOfWork,
            request.TenantId,
            request.ProjectId,
            request.PageId,
            cancellationToken);
        if (page.Status == Sddp.Abstractions.Enums.KnowledgePageStatus.Archived)
        {
            throw new ValidationException("page", "Archived Knowledge pages are read-only.");
        }

        KnowledgeCommandHelpers.EnsureRevision(page, request.ExpectedRevision);
        var title = KnowledgeCommandHelpers.NormalizeTitle(request.Dto.Title);
        var slug = KnowledgeCommandHelpers.NormalizeSlug(
            string.IsNullOrWhiteSpace(request.Dto.Slug) ? title : request.Dto.Slug);
        await KnowledgeCommandHelpers.EnsureSlugAvailableAsync(
            _unitOfWork,
            page,
            page.ParentId,
            slug,
            cancellationToken);

        var content = KnowledgeContentParser.Parse(request.Dto.Content);
        await KnowledgeCommandHelpers.ValidateReferencesAsync(
            _unitOfWork,
            request.TenantId,
            request.ProjectId,
            request.UserId,
            content.References,
            cancellationToken);
        var tags = KnowledgeContentParser.NormalizeTags(request.Dto.Tags);
        var tagsJson = KnowledgeContentParser.SerializeTags(tags);
        var noChange = page.Title == title
            && page.Slug == slug
            && page.ContentJson == content.Json
            && page.TagsJson == tagsJson;
        if (noChange)
        {
            return KnowledgeMapping.ToDto(page);
        }

        page.UpdateContent(
            title,
            slug,
            content.Json,
            content.PlainText,
            tagsJson,
            request.UserId);
        await KnowledgeCommandHelpers.SynchronizeLinksAsync(
            _unitOfWork,
            page,
            content.References,
            cancellationToken);
        await _unitOfWork.Repository<KnowledgePageVersion>().AddAsync(
            new KnowledgePageVersion(page, request.UserId, request.Dto.ChangeSummary?.Trim()),
            cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        request.AuditLog = new AuditLogRequest(
            request.UserId,
            "update",
            "knowledge_page",
            page.Id,
            new { page.Title, page.Revision, request.Dto.ChangeSummary },
            request.TenantId,
            request.ProjectId);

        return KnowledgeMapping.ToDto(page);
    }
}
