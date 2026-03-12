using Sddp.Abstractions.DTOs;
using Sddp.Abstractions.Enums;
using Sddp.Abstractions.Interfaces;
using Sddp.Abstractions.ValueObjects;
using Sddp.Domain.Entities;

namespace Sddp.Application.Features.Dashboard;

internal static class DashboardHelpers
{
    internal static async Task<MyOverviewDto> BuildOverviewAsync(
        IUnitOfWork unitOfWork,
        GlobalUniqueId tenantId,
        GlobalUniqueId userId,
        CancellationToken cancellationToken)
    {
        // Get project IDs where the user is a member
        var memberRepo = unitOfWork.Repository<ProjectMember>();
        var memberships = await (memberRepo.FindAsync(
            pm => pm.UserId == userId, cancellationToken)).ConfigureAwait(false);
        var projectIds = memberships.Select(pm => pm.ProjectId).Distinct().ToList();

        if (projectIds.Count == 0)
        {
            return new MyOverviewDto(
                Tasks: new DashboardStatDto(0, 0),
                Conversations: new DashboardStatDto(0, 0),
                Specs: new DashboardStatDto(0, 0),
                Requirements: new DashboardStatDto(0, 0),
                Glossary: new DashboardStatDto(0, 0),
                Artifacts: new DashboardStatDto(0, 0));
        }

        var specRepo = unitOfWork.Repository<Spec>();
        var reqRepo = unitOfWork.Repository<Requirement>();
        var conversationRepo = unitOfWork.Repository<Conversation>();
        var glossaryRepo = unitOfWork.Repository<GlossaryTerm>();
        var artifactRepo = unitOfWork.Repository<ArtifactTracking>();

        var totalSpecs = await (specRepo.CountAsync(
            s => s.TenantId == tenantId && s.ValidTo == null && projectIds.Contains(s.ProjectId), cancellationToken)).ConfigureAwait(false);
        var specsInReview = await (specRepo.CountAsync(
            s => s.TenantId == tenantId && s.ValidTo == null && s.Status == SpecStatus.InReview && projectIds.Contains(s.ProjectId), cancellationToken)).ConfigureAwait(false);

        var totalReqs = await (reqRepo.CountAsync(
            r => r.TenantId == tenantId && r.ValidTo == null && projectIds.Contains(r.ProjectId), cancellationToken)).ConfigureAwait(false);
        var draftReqs = await (reqRepo.CountAsync(
            r => r.TenantId == tenantId && r.ValidTo == null && r.Status == RequirementStatus.Draft && projectIds.Contains(r.ProjectId), cancellationToken)).ConfigureAwait(false);

        // Conversation.ProjectId is nullable (GlobalUniqueId?) — Contains() with nullable
        // causes Npgsql array type mapping error, so filter in memory after tenant-scoped query
        var projectIdSet = projectIds.ToHashSet();
        var tenantConversations = await (conversationRepo.FindAsync(
            d => d.TenantId == tenantId && d.ProjectId != null, cancellationToken)).ConfigureAwait(false);
        var myConversations = tenantConversations
            .Where(c => c.ProjectId.HasValue && projectIdSet.Contains(c.ProjectId.Value))
            .ToList();
        var totalConversations = myConversations.Count;
        var activeConversations = myConversations.Count(c => !c.IsArchived);

        var totalGlossary = await (glossaryRepo.CountAsync(
            g => g.TenantId == tenantId && projectIds.Contains(g.ProjectId), cancellationToken)).ConfigureAwait(false);
        var pendingGlossary = await (glossaryRepo.CountAsync(
            g => g.TenantId == tenantId && g.Status == GlossaryTermStatus.Draft && projectIds.Contains(g.ProjectId), cancellationToken)).ConfigureAwait(false);

        var totalArtifacts = await (artifactRepo.CountAsync(
            a => a.TenantId == tenantId && projectIds.Contains(a.ProjectId), cancellationToken)).ConfigureAwait(false);

        return new MyOverviewDto(
            Tasks: new DashboardStatDto(0, 0),
            Conversations: new DashboardStatDto(totalConversations, activeConversations),
            Specs: new DashboardStatDto(totalSpecs, specsInReview),
            Requirements: new DashboardStatDto(totalReqs, draftReqs),
            Glossary: new DashboardStatDto(totalGlossary, pendingGlossary),
            Artifacts: new DashboardStatDto(totalArtifacts, 0));
    }

    internal static AuditLogEntry MapAuditLogEntry(AuditLog log)
    {
        return new AuditLogEntry(
            Id: log.Id,
            ActorId: log.ActorId,
            Action: log.Action,
            ResourceType: log.ResourceType,
            ResourceId: log.ResourceId,
            Payload: log.Payload,
            CreatedAt: log.CreatedAt);
    }

    internal static MyTasksDto BuildEmptyTasks() => new(
        Tasks: [],
        Total: 0,
        ToDo: 0,
        InProgress: 0,
        Done: 0);

    internal static MyNotificationsDto BuildEmptyNotifications() => new(
        Notifications: [],
        UnreadCount: 0);

    internal static HealthCheckDto BuildDefaultHealth() => new(
        Status: "Healthy",
        Services: new List<ServiceHealthDto>
        {
            new("API Server", "Healthy", null, 45),
            new("Database", "Healthy", null, 12),
            new("Redis Cache", "Healthy", null, 3)
        });
}
