using System.Text;
using Sddp.Abstractions.Enums;
using Sddp.Abstractions.Exceptions;
using Sddp.Abstractions.Interfaces;
using Sddp.Abstractions.ValueObjects;
using Sddp.Domain.Entities;

namespace Sddp.Application.Features.Knowledge.Commands;

internal static class KnowledgeCommandHelpers
{
    public static async Task EnsureProjectAsync(
        IUnitOfWork unitOfWork,
        GlobalUniqueId tenantId,
        GlobalUniqueId projectId,
        CancellationToken cancellationToken)
    {
        var projects = await unitOfWork.Repository<Project>().FindAsync(
            project => project.Id == projectId && project.TenantId == tenantId,
            cancellationToken);
        if (projects.Count == 0)
        {
            throw new NotFoundException("Project", projectId.ToString());
        }
    }

    public static async Task<KnowledgePage> GetPageAsync(
        IUnitOfWork unitOfWork,
        GlobalUniqueId tenantId,
        GlobalUniqueId projectId,
        GlobalUniqueId pageId,
        CancellationToken cancellationToken)
    {
        var pages = await unitOfWork.Repository<KnowledgePage>().FindAsync(
            page => page.Id == pageId
                && page.TenantId == tenantId
                && page.ProjectId == projectId
                && page.IsActive,
            cancellationToken);
        return pages.FirstOrDefault()
            ?? throw new NotFoundException("KnowledgePage", pageId.ToString());
    }

    public static async Task<KnowledgePage?> GetParentAsync(
        IUnitOfWork unitOfWork,
        GlobalUniqueId tenantId,
        GlobalUniqueId projectId,
        GlobalUniqueId? parentId,
        CancellationToken cancellationToken)
    {
        if (!parentId.HasValue)
        {
            return null;
        }

        var parent = await GetPageAsync(
            unitOfWork,
            tenantId,
            projectId,
            parentId.Value,
            cancellationToken);
        if (parent.Status == KnowledgePageStatus.Archived)
        {
            throw new ValidationException("parentId", "An archived page cannot be used as a parent.");
        }

        return parent;
    }

    public static async Task<string> CreateAvailableSlugAsync(
        IUnitOfWork unitOfWork,
        GlobalUniqueId tenantId,
        GlobalUniqueId projectId,
        GlobalUniqueId? parentId,
        string title,
        CancellationToken cancellationToken)
    {
        var baseSlug = NormalizeSlug(title);
        var siblings = await unitOfWork.Repository<KnowledgePage>().FindAsync(
            page => page.TenantId == tenantId
                && page.ProjectId == projectId
                && page.ParentId == parentId
                && page.IsActive,
            cancellationToken);
        var used = siblings
            .Select(page => page.Slug)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        if (!used.Contains(baseSlug))
        {
            return baseSlug;
        }

        for (var suffix = 2; suffix <= 10_000; suffix++)
        {
            var suffixText = $"-{suffix}";
            var prefixLength = Math.Min(baseSlug.Length, 200 - suffixText.Length);
            var candidate = $"{baseSlug[..prefixLength]}{suffixText}";
            if (!used.Contains(candidate))
            {
                return candidate;
            }
        }

        throw new ConflictException("Unable to allocate a unique page slug.");
    }

    public static async Task EnsureSlugAvailableAsync(
        IUnitOfWork unitOfWork,
        KnowledgePage page,
        GlobalUniqueId? parentId,
        string slug,
        CancellationToken cancellationToken)
    {
        var conflicts = await unitOfWork.Repository<KnowledgePage>().FindAsync(
            candidate => candidate.TenantId == page.TenantId
                && candidate.ProjectId == page.ProjectId
                && candidate.ParentId == parentId
                && candidate.Id != page.Id
                && candidate.IsActive
                && candidate.Slug.ToLower() == slug.ToLower(),
            cancellationToken);
        if (conflicts.Count > 0)
        {
            throw new ConflictException(
                $"Knowledge page slug '{slug}' already exists under the selected parent.");
        }
    }

    public static string NormalizeTitle(string title)
    {
        var value = title.Trim();
        if (value.Length is < 1 or > 200)
        {
            throw new ValidationException("title", "Knowledge page title must contain 1 to 200 characters.");
        }

        return value;
    }

    public static string NormalizeSlug(string value)
    {
        var source = value.Trim().ToLowerInvariant();
        var slug = new StringBuilder(source.Length);
        var pendingSeparator = false;

        foreach (var character in source)
        {
            if (char.IsLetterOrDigit(character))
            {
                if (pendingSeparator && slug.Length > 0)
                {
                    slug.Append('-');
                }

                slug.Append(character);
                pendingSeparator = false;
            }
            else
            {
                pendingSeparator = slug.Length > 0;
            }

            if (slug.Length >= 200)
            {
                break;
            }
        }

        return slug.Length == 0 ? "page" : slug.ToString().TrimEnd('-');
    }

    /// <summary>
    /// Validates that every mention target exists in the project and is visible to the author.
    /// Visibility is resolved by <see cref="KnowledgeTargetVisibility"/> so that a Decision in a
    /// private conversation cannot be mentioned by a non-member.
    /// </summary>
    public static async Task ValidateReferencesAsync(
        IUnitOfWork unitOfWork,
        GlobalUniqueId tenantId,
        GlobalUniqueId projectId,
        GlobalUniqueId userId,
        IReadOnlyList<KnowledgeReference> references,
        CancellationToken cancellationToken)
    {
        foreach (var reference in references)
        {
            var valid = await KnowledgeTargetVisibility.IsTargetVisibleAsync(
                unitOfWork,
                tenantId,
                projectId,
                userId,
                reference.TargetType,
                reference.TargetId,
                cancellationToken);

            if (!valid)
            {
                // The message deliberately does not distinguish "does not exist" from "not visible"
                // so that it cannot be used to probe private conversations.
                throw new ValidationException(
                    "content",
                    $"Referenced {reference.TargetType} '{reference.TargetId}' is unavailable in this project.");
            }
        }
    }

    public static async Task SynchronizeLinksAsync(
        IUnitOfWork unitOfWork,
        KnowledgePage page,
        IReadOnlyList<KnowledgeReference> desiredReferences,
        CancellationToken cancellationToken)
    {
        var repository = unitOfWork.Repository<KnowledgePageLink>();
        var existing = await repository.FindIncludingInactiveAsync(
            link => link.PageId == page.Id
                && link.TenantId == page.TenantId
                && link.ProjectId == page.ProjectId,
            cancellationToken);
        var desired = desiredReferences
            .Select(reference => (reference.TargetType, reference.TargetId))
            .ToHashSet();

        foreach (var link in existing)
        {
            var isDesired = desired.Contains((link.TargetType, link.TargetId));
            if (!isDesired && link.IsActive)
            {
                link.Deactivate();
            }
            else if (isDesired && !link.IsActive)
            {
                link.Activate();
            }
        }

        var existingKeys = existing
            .Where(link => link.IsActive)
            .Select(link => (link.TargetType, link.TargetId))
            .ToHashSet();
        foreach (var reference in desiredReferences)
        {
            if (!existingKeys.Contains((reference.TargetType, reference.TargetId)))
            {
                await repository.AddAsync(
                    new KnowledgePageLink(
                        page.TenantId,
                        page.ProjectId,
                        page.Id,
                        reference.TargetType,
                        reference.TargetId),
                    cancellationToken);
            }
        }
    }

    public static void EnsureRevision(KnowledgePage page, int expectedRevision)
    {
        if (page.Revision != expectedRevision)
        {
            throw new KnowledgePageEditConflictException(expectedRevision, page.Revision);
        }
    }

}
