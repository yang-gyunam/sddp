using MediatR;
using Sddp.Abstractions.DTOs;
using Sddp.Abstractions.Enums;
using Sddp.Abstractions.Exceptions;
using Sddp.Abstractions.Interfaces;
using Sddp.Abstractions.ValueObjects;
using Sddp.Application.Requests;
using Sddp.Domain.Entities;

namespace Sddp.Application.Features.Knowledge.Commands;

public sealed record MoveKnowledgePageCommand(
    GlobalUniqueId TenantId,
    GlobalUniqueId ProjectId,
    GlobalUniqueId PageId,
    GlobalUniqueId UserId,
    int ExpectedRevision,
    MoveKnowledgePageDto Dto)
    : ICommand<KnowledgePageDto>, IAuditableRequest<KnowledgePageDto>
{
    public AuditLogRequest? AuditLog { get; set; }
    public AuditLogRequest? GetAuditLogRequest(KnowledgePageDto response) => AuditLog;
}

public sealed class MoveKnowledgePageCommandHandler
    : IRequestHandler<MoveKnowledgePageCommand, KnowledgePageDto>
{
    private const int MaximumDepth = 10;
    private readonly IUnitOfWork _unitOfWork;

    public MoveKnowledgePageCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<KnowledgePageDto> Handle(
        MoveKnowledgePageCommand request,
        CancellationToken cancellationToken)
    {
        if (request.Dto.Position < 0)
        {
            throw new ValidationException("position", "Page position must be zero or greater.");
        }

        var page = await KnowledgeCommandHelpers.GetPageAsync(
            _unitOfWork,
            request.TenantId,
            request.ProjectId,
            request.PageId,
            cancellationToken);
        KnowledgeCommandHelpers.EnsureRevision(page, request.ExpectedRevision);
        if (page.Status == KnowledgePageStatus.Archived)
        {
            throw new ValidationException("page", "Archived Knowledge pages are read-only.");
        }

        GlobalUniqueId? parentId = null;
        if (!string.IsNullOrWhiteSpace(request.Dto.ParentId))
        {
            if (!GlobalUniqueId.TryParse(request.Dto.ParentId, out var parsedParentId))
            {
                throw new ValidationException("parentId", "Parent ID is invalid.");
            }

            await KnowledgeCommandHelpers.GetParentAsync(
                _unitOfWork,
                request.TenantId,
                request.ProjectId,
                parsedParentId,
                cancellationToken);
            parentId = parsedParentId;
        }

        var pages = await _unitOfWork.Repository<KnowledgePage>().FindAsync(
            candidate => candidate.TenantId == request.TenantId
                && candidate.ProjectId == request.ProjectId,
            cancellationToken);
        var byId = pages.ToDictionary(candidate => candidate.Id);
        var parentDepth = 0;
        var cursor = parentId;
        while (cursor.HasValue)
        {
            if (cursor.Value == page.Id)
            {
                throw new KnowledgePageCycleException();
            }

            parentDepth++;
            cursor = byId.TryGetValue(cursor.Value, out var parent)
                ? parent.ParentId
                : null;
        }

        var subtreeHeight = CalculateSubtreeHeight(page.Id, pages);
        if (parentDepth + subtreeHeight > MaximumDepth)
        {
            throw new ValidationException("parentId", $"Knowledge Page depth cannot exceed {MaximumDepth}.");
        }

        await KnowledgeCommandHelpers.EnsureSlugAvailableAsync(
            _unitOfWork,
            page,
            parentId,
            page.Slug,
            cancellationToken);
        page.Move(parentId, request.Dto.Position, request.UserId);
        await _unitOfWork.Repository<KnowledgePageVersion>().AddAsync(
            new KnowledgePageVersion(page, request.UserId, "Move page"),
            cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        request.AuditLog = new AuditLogRequest(
            request.UserId,
            "move",
            "knowledge_page",
            page.Id,
            new { page.ParentId, page.Position },
            request.TenantId,
            request.ProjectId);
        return KnowledgeMapping.ToDto(page);
    }

    private static int CalculateSubtreeHeight(
        GlobalUniqueId pageId,
        IReadOnlyList<KnowledgePage> pages)
    {
        var children = pages
            .Where(page => page.ParentId == pageId)
            .ToArray();
        return children.Length == 0
            ? 1
            : 1 + children.Max(child => CalculateSubtreeHeight(child.Id, pages));
    }
}

public sealed record PublishKnowledgePageCommand(
    GlobalUniqueId TenantId,
    GlobalUniqueId ProjectId,
    GlobalUniqueId PageId,
    GlobalUniqueId UserId,
    int ExpectedRevision)
    : ICommand<KnowledgePageDto>, IAuditableRequest<KnowledgePageDto>
{
    public AuditLogRequest? AuditLog { get; set; }
    public AuditLogRequest? GetAuditLogRequest(KnowledgePageDto response) => AuditLog;
}

public sealed class PublishKnowledgePageCommandHandler
    : IRequestHandler<PublishKnowledgePageCommand, KnowledgePageDto>
{
    private readonly IUnitOfWork _unitOfWork;

    public PublishKnowledgePageCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<KnowledgePageDto> Handle(
        PublishKnowledgePageCommand request,
        CancellationToken cancellationToken)
    {
        var page = await KnowledgeCommandHelpers.GetPageAsync(
            _unitOfWork, request.TenantId, request.ProjectId, request.PageId, cancellationToken);
        KnowledgeCommandHelpers.EnsureRevision(page, request.ExpectedRevision);
        if (page.Status == KnowledgePageStatus.Archived)
        {
            throw new ValidationException("page", "An archived page must be restored before it can be published.");
        }

        if (page.Status == KnowledgePageStatus.Published)
        {
            return KnowledgeMapping.ToDto(page);
        }

        page.Publish(request.UserId);
        await _unitOfWork.Repository<KnowledgePageVersion>().AddAsync(
            new KnowledgePageVersion(page, request.UserId, "Publish"),
            cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        request.AuditLog = Audit(request, page, "publish");
        return KnowledgeMapping.ToDto(page);
    }

    private static AuditLogRequest Audit(
        PublishKnowledgePageCommand request,
        KnowledgePage page,
        string action) =>
        new(
            request.UserId,
            action,
            "knowledge_page",
            page.Id,
            new { page.Status, page.Revision },
            request.TenantId,
            request.ProjectId);
}

public sealed record ArchiveKnowledgePageCommand(
    GlobalUniqueId TenantId,
    GlobalUniqueId ProjectId,
    GlobalUniqueId PageId,
    GlobalUniqueId UserId,
    int ExpectedRevision,
    ArchiveKnowledgePageDto Dto)
    : ICommand<KnowledgePageDto>, IAuditableRequest<KnowledgePageDto>
{
    public AuditLogRequest? AuditLog { get; set; }
    public AuditLogRequest? GetAuditLogRequest(KnowledgePageDto response) => AuditLog;
}

public sealed class ArchiveKnowledgePageCommandHandler
    : IRequestHandler<ArchiveKnowledgePageCommand, KnowledgePageDto>
{
    private readonly IUnitOfWork _unitOfWork;

    public ArchiveKnowledgePageCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<KnowledgePageDto> Handle(
        ArchiveKnowledgePageCommand request,
        CancellationToken cancellationToken)
    {
        var page = await KnowledgeCommandHelpers.GetPageAsync(
            _unitOfWork, request.TenantId, request.ProjectId, request.PageId, cancellationToken);
        KnowledgeCommandHelpers.EnsureRevision(page, request.ExpectedRevision);
        if (page.Status == KnowledgePageStatus.Archived)
        {
            return KnowledgeMapping.ToDto(page);
        }

        var pages = await _unitOfWork.Repository<KnowledgePage>().FindAsync(
            candidate => candidate.TenantId == request.TenantId
                && candidate.ProjectId == request.ProjectId,
            cancellationToken);
        var affected = request.Dto.IncludeDescendants
            ? DescendantsOf(page.Id, pages).Prepend(page).ToArray()
            : [page];

        if (!request.Dto.IncludeDescendants)
        {
            foreach (var child in pages.Where(candidate => candidate.ParentId == page.Id))
            {
                await KnowledgeCommandHelpers.EnsureSlugAvailableAsync(
                    _unitOfWork,
                    child,
                    page.ParentId,
                    child.Slug,
                    cancellationToken);
                child.Move(page.ParentId, child.Position, request.UserId);
                await _unitOfWork.Repository<KnowledgePageVersion>().AddAsync(
                    new KnowledgePageVersion(
                        child,
                        request.UserId,
                        "Reparent before parent archive"),
                    cancellationToken);
            }
        }

        foreach (var affectedPage in affected)
        {
            if (affectedPage.Status == KnowledgePageStatus.Archived)
            {
                continue;
            }

            affectedPage.Archive(request.UserId);
            await _unitOfWork.Repository<KnowledgePageVersion>().AddAsync(
                new KnowledgePageVersion(affectedPage, request.UserId, "Archive"),
                cancellationToken);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        request.AuditLog = new AuditLogRequest(
            request.UserId,
            "archive",
            "knowledge_page",
            page.Id,
            new { page.Revision, request.Dto.IncludeDescendants, AffectedCount = affected.Length },
            request.TenantId,
            request.ProjectId);
        return KnowledgeMapping.ToDto(page);
    }

    private static IEnumerable<KnowledgePage> DescendantsOf(
        GlobalUniqueId pageId,
        IReadOnlyList<KnowledgePage> pages)
    {
        foreach (var child in pages.Where(candidate => candidate.ParentId == pageId))
        {
            yield return child;
            foreach (var descendant in DescendantsOf(child.Id, pages))
            {
                yield return descendant;
            }
        }
    }
}

public sealed record RestoreKnowledgePageCommand(
    GlobalUniqueId TenantId,
    GlobalUniqueId ProjectId,
    GlobalUniqueId PageId,
    GlobalUniqueId UserId,
    int ExpectedRevision)
    : ICommand<KnowledgePageDto>, IAuditableRequest<KnowledgePageDto>
{
    public AuditLogRequest? AuditLog { get; set; }
    public AuditLogRequest? GetAuditLogRequest(KnowledgePageDto response) => AuditLog;
}

public sealed class RestoreKnowledgePageCommandHandler
    : IRequestHandler<RestoreKnowledgePageCommand, KnowledgePageDto>
{
    private readonly IUnitOfWork _unitOfWork;

    public RestoreKnowledgePageCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<KnowledgePageDto> Handle(
        RestoreKnowledgePageCommand request,
        CancellationToken cancellationToken)
    {
        var page = await KnowledgeCommandHelpers.GetPageAsync(
            _unitOfWork, request.TenantId, request.ProjectId, request.PageId, cancellationToken);
        KnowledgeCommandHelpers.EnsureRevision(page, request.ExpectedRevision);
        if (page.Status != KnowledgePageStatus.Archived)
        {
            return KnowledgeMapping.ToDto(page);
        }

        if (page.ParentId.HasValue)
        {
            var parent = await KnowledgeCommandHelpers.GetPageAsync(
                _unitOfWork,
                request.TenantId,
                request.ProjectId,
                page.ParentId.Value,
                cancellationToken);
            if (parent.Status == KnowledgePageStatus.Archived)
            {
                await KnowledgeCommandHelpers.EnsureSlugAvailableAsync(
                    _unitOfWork,
                    page,
                    null,
                    page.Slug,
                    cancellationToken);
                page.Move(null, page.Position, request.UserId, incrementRevision: false);
            }
        }

        page.Restore(request.UserId);
        await _unitOfWork.Repository<KnowledgePageVersion>().AddAsync(
            new KnowledgePageVersion(page, request.UserId, "Restore archived page"),
            cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        request.AuditLog = new AuditLogRequest(
            request.UserId,
            "restore",
            "knowledge_page",
            page.Id,
            new { page.Status, page.Revision },
            request.TenantId,
            request.ProjectId);
        return KnowledgeMapping.ToDto(page);
    }
}

public sealed record RestoreKnowledgePageVersionCommand(
    GlobalUniqueId TenantId,
    GlobalUniqueId ProjectId,
    GlobalUniqueId PageId,
    int Revision,
    GlobalUniqueId UserId,
    int ExpectedRevision)
    : ICommand<KnowledgePageDto>, IAuditableRequest<KnowledgePageDto>
{
    public AuditLogRequest? AuditLog { get; set; }
    public AuditLogRequest? GetAuditLogRequest(KnowledgePageDto response) => AuditLog;
}

public sealed class RestoreKnowledgePageVersionCommandHandler
    : IRequestHandler<RestoreKnowledgePageVersionCommand, KnowledgePageDto>
{
    private readonly IUnitOfWork _unitOfWork;

    public RestoreKnowledgePageVersionCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<KnowledgePageDto> Handle(
        RestoreKnowledgePageVersionCommand request,
        CancellationToken cancellationToken)
    {
        var page = await KnowledgeCommandHelpers.GetPageAsync(
            _unitOfWork, request.TenantId, request.ProjectId, request.PageId, cancellationToken);
        KnowledgeCommandHelpers.EnsureRevision(page, request.ExpectedRevision);
        if (page.Status == KnowledgePageStatus.Archived)
        {
            throw new ValidationException("page", "Restore the archived page before restoring a version.");
        }

        var versions = await _unitOfWork.Repository<KnowledgePageVersion>().FindAsync(
            version => version.PageId == page.Id
                && version.TenantId == request.TenantId
                && version.ProjectId == request.ProjectId
                && version.Revision == request.Revision,
            cancellationToken);
        var version = versions.FirstOrDefault()
            ?? throw new NotFoundException(
                "KnowledgePageVersion",
                $"{request.PageId}:{request.Revision}");

        using var document = System.Text.Json.JsonDocument.Parse(version.ContentJson);
        var content = KnowledgeContentParser.Parse(document.RootElement);
        await KnowledgeCommandHelpers.ValidateReferencesAsync(
            _unitOfWork,
            request.TenantId,
            request.ProjectId,
            request.UserId,
            content.References,
            cancellationToken);
        await KnowledgeCommandHelpers.EnsureSlugAvailableAsync(
            _unitOfWork,
            page,
            page.ParentId,
            version.Slug,
            cancellationToken);

        page.RestoreVersion(version, request.UserId);
        await KnowledgeCommandHelpers.SynchronizeLinksAsync(
            _unitOfWork,
            page,
            content.References,
            cancellationToken);
        await _unitOfWork.Repository<KnowledgePageVersion>().AddAsync(
            new KnowledgePageVersion(page, request.UserId, $"Restore revision {request.Revision}"),
            cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        request.AuditLog = new AuditLogRequest(
            request.UserId,
            "restore_version",
            "knowledge_page",
            page.Id,
            new { RestoredRevision = request.Revision, page.Revision },
            request.TenantId,
            request.ProjectId);
        return KnowledgeMapping.ToDto(page);
    }
}
