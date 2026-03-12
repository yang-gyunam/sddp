using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Sddp.Domain.Entities;

namespace Sddp.Infrastructure.Persistence.Contexts;

/// <summary>
/// Main SDDP DbContext for the public runtime (command side, EF Core).
/// </summary>
public class SddpDbContext : DbContext
{
    public SddpDbContext(DbContextOptions<SddpDbContext> options) : base(options)
    {
    }

    public DbSet<Person> Persons => Set<Person>();
    public DbSet<User> Users => Set<User>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<Permission> Permissions => Set<Permission>();
    public DbSet<UserRole> UserRoles => Set<UserRole>();
    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();

    public DbSet<Project> Projects => Set<Project>();
    public DbSet<ProjectMember> ProjectMembers => Set<ProjectMember>();

    public DbSet<LegacyIdMapping> LegacyIdMappings => Set<LegacyIdMapping>();

    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    public DbSet<Conversation> Conversations => Set<Conversation>();
    public DbSet<Channel> Channels => Set<Channel>();
    public DbSet<Forum> Forums => Set<Forum>();
    public DbSet<DirectMessage> DirectMessages => Set<DirectMessage>();
    public DbSet<Topic> Topics => Set<Topic>();
    public DbSet<ConversationMember> ConversationMembers => Set<ConversationMember>();
    public DbSet<Message> Messages => Set<Message>();
    public DbSet<UserReadStatus> UserReadStatuses => Set<UserReadStatus>();
    public DbSet<UserConversationSettings> UserConversationSettings => Set<UserConversationSettings>();

    public DbSet<Requirement> Requirements => Set<Requirement>();

    public DbSet<Spec> Specs => Set<Spec>();
    public DbSet<SignOff> SignOffs => Set<SignOff>();
    public DbSet<Alternative> Alternatives => Set<Alternative>();

    public DbSet<Relationship> Relationships => Set<Relationship>();
    public DbSet<GlossaryTerm> GlossaryTerms => Set<GlossaryTerm>();

    public DbSet<EntityMetadata> EntityMetadata => Set<EntityMetadata>();
    public DbSet<FieldMetadata> FieldMetadata => Set<FieldMetadata>();
    public DbSet<EntityRelationshipMetadata> EntityRelationshipMetadata => Set<EntityRelationshipMetadata>();

    public DbSet<ArtifactTracking> ArtifactTrackings => Set<ArtifactTracking>();
    public DbSet<ArtifactToSpecMapping> ArtifactToSpecMappings => Set<ArtifactToSpecMapping>();

    public DbSet<TaskItem> TaskItems => Set<TaskItem>();
    public DbSet<TaskAcceptanceCriterion> TaskAcceptanceCriteria => Set<TaskAcceptanceCriterion>();
    public DbSet<TaskLinkedItem> TaskLinkedItems => Set<TaskLinkedItem>();
    public DbSet<TaskTimeLog> TaskTimeLogs => Set<TaskTimeLog>();
    public DbSet<TaskCategory> TaskCategories => Set<TaskCategory>();

    public DbSet<EffortAllocation> EffortAllocations => Set<EffortAllocation>();
    public DbSet<Worklog> Worklogs => Set<Worklog>();
    public DbSet<WorkingDay> WorkingDays => Set<WorkingDay>();

    public DbSet<SystemConfig> SystemConfigs => Set<SystemConfig>();

    public DbSet<SlaPolicy> SlaPolicies => Set<SlaPolicy>();
    public DbSet<SlaNotification> SlaNotifications => Set<SlaNotification>();

    public DbSet<Notification> Notifications => Set<Notification>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Ignore<CultureInfo>();
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(SddpDbContext).Assembly);
    }
}
