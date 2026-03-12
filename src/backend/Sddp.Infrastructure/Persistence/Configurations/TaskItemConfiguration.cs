using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sddp.Abstractions.Constants;
using Sddp.Abstractions.ValueObjects;
using Sddp.Domain.Entities;

namespace Sddp.Infrastructure.Persistence.Configurations;

public class TaskItemConfiguration : IEntityTypeConfiguration<TaskItem>
{
    public void Configure(EntityTypeBuilder<TaskItem> builder)
    {
        builder.ToTable("tasks");

        builder.HasKey(t => t.Id);

        builder.Property(t => t.Id)
            .HasConversion(
                id => id.ToGuid(),
                guid => GlobalUniqueId.FromGuid(guid))
            .HasColumnName("id");

        builder.Property(t => t.TenantId)
            .HasConversion(
                id => id.ToGuid(),
                guid => GlobalUniqueId.FromGuid(guid))
            .HasColumnName("tenant_id")
            .IsRequired();

        builder.Property(t => t.ProjectId)
            .HasConversion(
                id => id.HasValue ? id.Value.ToGuid() : (Guid?)null,
                guid => guid.HasValue ? GlobalUniqueId.FromGuid(guid.Value) : null)
            .HasColumnName("project_id");

        builder.Property(t => t.Title)
            .HasColumnName("title")
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(t => t.Description)
            .HasColumnName("description")
            .HasColumnType("text");

        builder.Property(t => t.Status)
            .HasColumnName("status_code_id")
            .HasConversion(
                e => WellKnownCodes.TaskItemStatusToId[e],
                id => WellKnownCodes.IdToTaskItemStatus[id])
            .IsRequired();

        builder.Property(t => t.Priority)
            .HasColumnName("priority_code_id")
            .HasConversion(
                e => WellKnownCodes.TaskItemPriorityToId[e],
                id => WellKnownCodes.IdToTaskItemPriority[id])
            .IsRequired();

        builder.Property(t => t.CategoryId)
            .HasConversion(
                id => id.HasValue ? id.Value.ToGuid() : (Guid?)null,
                guid => guid.HasValue ? GlobalUniqueId.FromGuid(guid.Value) : null)
            .HasColumnName("category_id");

        builder.Property(t => t.AssigneeId)
            .HasConversion(
                id => id.HasValue ? id.Value.ToGuid() : (Guid?)null,
                guid => guid.HasValue ? GlobalUniqueId.FromGuid(guid.Value) : null)
            .HasColumnName("assignee_id");

        builder.Property(t => t.CreatorId)
            .HasConversion(
                id => id.ToGuid(),
                guid => GlobalUniqueId.FromGuid(guid))
            .HasColumnName("creator_id")
            .IsRequired();

        builder.Property(t => t.EstimatedHours)
            .HasColumnName("estimated_hours")
            .HasColumnType("decimal(10,2)");

        builder.Property(t => t.ActualHours)
            .HasColumnName("actual_hours")
            .HasColumnType("decimal(10,2)");

        builder.Property(t => t.CompletedAt)
            .HasConversion(
                ts => ts.HasValue ? ts.Value.ToDateTimeOffset() : (DateTimeOffset?)null,
                dto => dto.HasValue ? Timestamp.FromDateTimeOffset(dto.Value) : null)
            .HasColumnName("completed_at");

        builder.Property(t => t.DueDate)
            .HasColumnName("due_date");

        builder.Property(t => t.SortOrder)
            .HasColumnName("sort_order")
            .HasDefaultValue(0);

        // AuditableEntityBase
        builder.Property(t => t.CreatedBy)
            .HasConversion(
                id => id.ToGuid(),
                guid => GlobalUniqueId.FromGuid(guid))
            .HasColumnName("created_by");

        builder.Property(t => t.UpdatedBy)
            .HasConversion(
                id => id.ToGuid(),
                guid => GlobalUniqueId.FromGuid(guid))
            .HasColumnName("updated_by");

        // EntityBase
        builder.Property(t => t.IsActive)
            .HasColumnName("is_active")
            .HasDefaultValue(true);

        builder.Property(t => t.CreatedAt)
            .HasConversion(
                ts => ts.ToDateTimeOffset(),
                dto => Timestamp.FromDateTimeOffset(dto))
            .HasColumnName("created_at");

        builder.Property(t => t.UpdatedAt)
            .HasConversion(
                ts => ts.ToDateTimeOffset(),
                dto => Timestamp.FromDateTimeOffset(dto))
            .HasColumnName("updated_at");

        // relationship
        builder.HasMany(t => t.AcceptanceCriteria)
            .WithOne(c => c.Task)
            .HasForeignKey(c => c.TaskId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(t => t.LinkedItems)
            .WithOne(l => l.Task)
            .HasForeignKey(l => l.TaskId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(t => t.TimeLogs)
            .WithOne(l => l.Task)
            .HasForeignKey(l => l.TaskId)
            .OnDelete(DeleteBehavior.Cascade);

        // Auto-include child collections for detail queries
        builder.Navigation(t => t.AcceptanceCriteria).AutoInclude();
        builder.Navigation(t => t.LinkedItems).AutoInclude();
        builder.Navigation(t => t.TimeLogs).AutoInclude();

        //
        builder.HasIndex(t => new { t.TenantId, t.ProjectId })
            .HasDatabaseName("ix_tasks_tenant_project");

        builder.HasIndex(t => t.Status)
            .HasDatabaseName("ix_tasks_status");

        builder.HasIndex(t => t.Priority)
            .HasDatabaseName("ix_tasks_priority");

        builder.HasIndex(t => t.AssigneeId)
            .HasDatabaseName("ix_tasks_assignee");

        builder.HasIndex(t => t.IsActive)
            .HasDatabaseName("ix_tasks_is_active");

        builder.HasIndex(t => new { t.Status, t.SortOrder })
            .HasDatabaseName("ix_tasks_sort_order");
    }
}

public class TaskAcceptanceCriterionConfiguration : IEntityTypeConfiguration<TaskAcceptanceCriterion>
{
    public void Configure(EntityTypeBuilder<TaskAcceptanceCriterion> builder)
    {
        builder.ToTable("task_acceptance_criteria");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.Id)
            .HasConversion(
                id => id.ToGuid(),
                guid => GlobalUniqueId.FromGuid(guid))
            .HasColumnName("id");

        builder.Property(c => c.TaskId)
            .HasConversion(
                id => id.ToGuid(),
                guid => GlobalUniqueId.FromGuid(guid))
            .HasColumnName("task_id")
            .IsRequired();

        builder.Property(c => c.Description)
            .HasColumnName("description")
            .HasColumnType("text")
            .IsRequired();

        builder.Property(c => c.Completed)
            .HasColumnName("completed")
            .HasDefaultValue(false);

        builder.Property(c => c.SortOrder)
            .HasColumnName("sort_order")
            .HasDefaultValue(0);

        builder.Property(c => c.CreatedAt)
            .HasConversion(
                ts => ts.ToDateTimeOffset(),
                dto => Timestamp.FromDateTimeOffset(dto))
            .HasColumnName("created_at");

        builder.Property(c => c.UpdatedAt)
            .HasConversion(
                ts => ts.ToDateTimeOffset(),
                dto => Timestamp.FromDateTimeOffset(dto))
            .HasColumnName("updated_at");

        // EntityBase inherited - ignore columns not in task_acceptance_criteria table
        builder.Ignore(c => c.IsActive);
        builder.Ignore(c => c.DomainEvents);

        builder.HasIndex(c => c.TaskId)
            .HasDatabaseName("ix_task_criteria_task");
    }
}

public class TaskLinkedItemConfiguration : IEntityTypeConfiguration<TaskLinkedItem>
{
    public void Configure(EntityTypeBuilder<TaskLinkedItem> builder)
    {
        builder.ToTable("task_linked_items");

        builder.HasKey(l => l.Id);

        builder.Property(l => l.Id)
            .HasConversion(
                id => id.ToGuid(),
                guid => GlobalUniqueId.FromGuid(guid))
            .HasColumnName("id");

        builder.Property(l => l.TaskId)
            .HasConversion(
                id => id.ToGuid(),
                guid => GlobalUniqueId.FromGuid(guid))
            .HasColumnName("task_id")
            .IsRequired();

        builder.Property(l => l.LinkedType)
            .HasColumnName("linked_type")
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(l => l.LinkedEntityId)
            .HasConversion(
                id => id.ToGuid(),
                guid => GlobalUniqueId.FromGuid(guid))
            .HasColumnName("linked_entity_id")
            .IsRequired();

        builder.Property(l => l.LinkedBy)
            .HasConversion(
                id => id.ToGuid(),
                guid => GlobalUniqueId.FromGuid(guid))
            .HasColumnName("linked_by")
            .IsRequired();

        builder.Property(l => l.LinkedAt)
            .HasConversion(
                ts => ts.ToDateTimeOffset(),
                dto => Timestamp.FromDateTimeOffset(dto))
            .HasColumnName("linked_at");

        // EntityBase inherited - ignore columns not in task_linked_items table
        builder.Ignore(l => l.IsActive);
        builder.Ignore(l => l.UpdatedAt);
        builder.Ignore(l => l.CreatedAt);
        builder.Ignore(l => l.DomainEvents);

        builder.HasIndex(l => l.TaskId)
            .HasDatabaseName("ix_task_links_task");

        builder.HasIndex(l => new { l.LinkedType, l.LinkedEntityId })
            .HasDatabaseName("ix_task_links_type_entity");

        builder.HasIndex(l => new { l.TaskId, l.LinkedType, l.LinkedEntityId })
            .IsUnique()
            .HasDatabaseName("uq_task_links_unique");
    }
}

public class TaskTimeLogConfiguration : IEntityTypeConfiguration<TaskTimeLog>
{
    public void Configure(EntityTypeBuilder<TaskTimeLog> builder)
    {
        builder.ToTable("task_time_logs");

        builder.HasKey(l => l.Id);

        builder.Property(l => l.Id)
            .HasConversion(
                id => id.ToGuid(),
                guid => GlobalUniqueId.FromGuid(guid))
            .HasColumnName("id");

        builder.Property(l => l.TaskId)
            .HasConversion(
                id => id.ToGuid(),
                guid => GlobalUniqueId.FromGuid(guid))
            .HasColumnName("task_id")
            .IsRequired();

        builder.Property(l => l.UserId)
            .HasConversion(
                id => id.ToGuid(),
                guid => GlobalUniqueId.FromGuid(guid))
            .HasColumnName("user_id")
            .IsRequired();

        builder.Property(l => l.LogDate)
            .HasColumnName("log_date")
            .IsRequired();

        builder.Property(l => l.Hours)
            .HasColumnName("hours")
            .HasColumnType("decimal(5,2)")
            .IsRequired();

        builder.Property(l => l.Description)
            .HasColumnName("description")
            .HasColumnType("text");

        builder.Property(l => l.CreatedAt)
            .HasConversion(
                ts => ts.ToDateTimeOffset(),
                dto => Timestamp.FromDateTimeOffset(dto))
            .HasColumnName("created_at");

        // EntityBase inherited - ignore columns not in task_time_logs table
        builder.Ignore(l => l.IsActive);
        builder.Ignore(l => l.UpdatedAt);
        builder.Ignore(l => l.DomainEvents);

        builder.HasIndex(l => l.TaskId)
            .HasDatabaseName("ix_task_time_logs_task");

        builder.HasIndex(l => l.UserId)
            .HasDatabaseName("ix_task_time_logs_user");

        builder.HasIndex(l => l.LogDate)
            .HasDatabaseName("ix_task_time_logs_date");
    }
}
