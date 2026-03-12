using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sddp.Abstractions.ValueObjects;
using Sddp.Domain.Entities;

namespace Sddp.Infrastructure.Persistence.Configurations;

public class EffortAllocationConfiguration : IEntityTypeConfiguration<EffortAllocation>
{
    public void Configure(EntityTypeBuilder<EffortAllocation> builder)
    {
        builder.ToTable("effort_allocations");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
            .HasConversion(
                id => id.ToGuid(),
                guid => GlobalUniqueId.FromGuid(guid))
            .HasColumnName("id");

        builder.Property(e => e.TenantId)
            .HasConversion(
                id => id.ToGuid(),
                guid => GlobalUniqueId.FromGuid(guid))
            .HasColumnName("tenant_id")
            .IsRequired();

        builder.Property(e => e.ProjectId)
            .HasConversion(
                id => id.ToGuid(),
                guid => GlobalUniqueId.FromGuid(guid))
            .HasColumnName("project_id")
            .IsRequired();

        builder.Property(e => e.UserId)
            .HasConversion(
                id => id.ToGuid(),
                guid => GlobalUniqueId.FromGuid(guid))
            .HasColumnName("user_id")
            .IsRequired();

        builder.Property(e => e.AllocationDate)
            .HasColumnName("allocation_date")
            .IsRequired();

        builder.Property(e => e.AllocatedHours)
            .HasColumnName("allocated_hours")
            .HasColumnType("decimal(4,2)")
            .IsRequired();

        builder.Property(e => e.CreatedBy)
            .HasConversion(
                id => id.ToGuid(),
                guid => GlobalUniqueId.FromGuid(guid))
            .HasColumnName("created_by")
            .IsRequired();

        builder.Property(e => e.UpdatedBy)
            .HasConversion(
                id => id.ToGuid(),
                guid => GlobalUniqueId.FromGuid(guid))
            .HasColumnName("updated_by")
            .IsRequired();

        builder.Property(e => e.CreatedAt)
            .HasConversion(
                ts => ts.ToDateTimeOffset(),
                dto => Timestamp.FromDateTimeOffset(dto))
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(e => e.UpdatedAt)
            .HasConversion(
                ts => ts.ToDateTimeOffset(),
                dto => Timestamp.FromDateTimeOffset(dto))
            .HasColumnName("updated_at")
            .IsRequired();

        builder.Property(e => e.IsActive)
            .HasColumnName("is_active")
            .HasDefaultValue(true)
            .IsRequired();

        // EntityBase inherited - ignore columns not in effort_allocations table
        builder.Ignore(e => e.DomainEvents);

        // Relationship
        builder.HasOne(e => e.User)
            .WithMany()
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        // Indexes
        builder.HasIndex(e => new { e.TenantId, e.ProjectId })
            .HasDatabaseName("idx_effort_allocations_tenant_project");

        builder.HasIndex(e => e.UserId)
            .HasDatabaseName("idx_effort_allocations_user");

        builder.HasIndex(e => e.AllocationDate)
            .HasDatabaseName("idx_effort_allocations_date");

        builder.HasIndex(e => new { e.ProjectId, e.AllocationDate })
            .HasDatabaseName("idx_effort_allocations_project_date");

        builder.HasIndex(e => new { e.ProjectId, e.UserId, e.AllocationDate })
            .IsUnique()
            .HasDatabaseName("uq_effort_allocations_unique");
    }
}

public class WorklogConfiguration : IEntityTypeConfiguration<Worklog>
{
    public void Configure(EntityTypeBuilder<Worklog> builder)
    {
        builder.ToTable("worklogs");

        builder.HasKey(w => w.Id);

        builder.Property(w => w.Id)
            .HasConversion(
                id => id.ToGuid(),
                guid => GlobalUniqueId.FromGuid(guid))
            .HasColumnName("id");

        builder.Property(w => w.TenantId)
            .HasConversion(
                id => id.ToGuid(),
                guid => GlobalUniqueId.FromGuid(guid))
            .HasColumnName("tenant_id")
            .IsRequired();

        builder.Property(w => w.ProjectId)
            .HasConversion(
                id => id.ToGuid(),
                guid => GlobalUniqueId.FromGuid(guid))
            .HasColumnName("project_id")
            .IsRequired();

        builder.Property(w => w.UserId)
            .HasConversion(
                id => id.ToGuid(),
                guid => GlobalUniqueId.FromGuid(guid))
            .HasColumnName("user_id")
            .IsRequired();

        builder.Property(w => w.WorkDate)
            .HasColumnName("work_date")
            .IsRequired();

        builder.Property(w => w.SpentHours)
            .HasColumnName("spent_hours")
            .HasColumnType("decimal(4,2)")
            .IsRequired();

        builder.Property(w => w.Note)
            .HasColumnName("note")
            .HasColumnType("text");

        builder.Property(w => w.TaskId)
            .HasConversion(
                id => id.HasValue ? id.Value.ToGuid() : (Guid?)null,
                guid => guid.HasValue ? GlobalUniqueId.FromGuid(guid.Value) : null)
            .HasColumnName("task_id");

        builder.Property(w => w.CreatedAt)
            .HasConversion(
                ts => ts.ToDateTimeOffset(),
                dto => Timestamp.FromDateTimeOffset(dto))
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(w => w.UpdatedAt)
            .HasConversion(
                ts => ts.ToDateTimeOffset(),
                dto => Timestamp.FromDateTimeOffset(dto))
            .HasColumnName("updated_at")
            .IsRequired();

        builder.Property(w => w.IsActive)
            .HasColumnName("is_active")
            .HasDefaultValue(true)
            .IsRequired();

        // EntityBase inherited - ignore columns not in worklogs table
        builder.Ignore(w => w.DomainEvents);

        // Relationships
        builder.HasOne(w => w.User)
            .WithMany()
            .HasForeignKey(w => w.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(w => w.Task)
            .WithMany()
            .HasForeignKey(w => w.TaskId)
            .OnDelete(DeleteBehavior.SetNull);

        // Indexes
        builder.HasIndex(w => new { w.TenantId, w.ProjectId })
            .HasDatabaseName("idx_worklogs_tenant_project");

        builder.HasIndex(w => w.UserId)
            .HasDatabaseName("idx_worklogs_user");

        builder.HasIndex(w => w.WorkDate)
            .HasDatabaseName("idx_worklogs_date");

        builder.HasIndex(w => new { w.ProjectId, w.WorkDate })
            .HasDatabaseName("idx_worklogs_project_date");
    }
}

public class WorkingDayConfiguration : IEntityTypeConfiguration<WorkingDay>
{
    public void Configure(EntityTypeBuilder<WorkingDay> builder)
    {
        builder.ToTable("working_days");

        builder.HasKey(wd => wd.Id);

        builder.Property(wd => wd.Id)
            .HasConversion(
                id => id.ToGuid(),
                guid => GlobalUniqueId.FromGuid(guid))
            .HasColumnName("id");

        builder.Property(wd => wd.TenantId)
            .HasConversion(
                id => id.ToGuid(),
                guid => GlobalUniqueId.FromGuid(guid))
            .HasColumnName("tenant_id")
            .IsRequired();

        builder.Property(wd => wd.ProjectId)
            .HasConversion(
                id => id.ToGuid(),
                guid => GlobalUniqueId.FromGuid(guid))
            .HasColumnName("project_id")
            .IsRequired();

        builder.Property(wd => wd.WorkDate)
            .HasColumnName("work_date")
            .IsRequired();

        builder.Property(wd => wd.DayType)
            .HasColumnName("day_type")
            .HasMaxLength(20)
            .HasDefaultValue("workday")
            .IsRequired();

        builder.Property(wd => wd.Note)
            .HasColumnName("note")
            .HasColumnType("text");

        builder.Property(wd => wd.CreatedBy)
            .HasConversion(
                id => id.ToGuid(),
                guid => GlobalUniqueId.FromGuid(guid))
            .HasColumnName("created_by")
            .IsRequired();

        builder.Property(wd => wd.UpdatedBy)
            .HasConversion(
                id => id.ToGuid(),
                guid => GlobalUniqueId.FromGuid(guid))
            .HasColumnName("updated_by")
            .IsRequired();

        builder.Property(wd => wd.CreatedAt)
            .HasConversion(
                ts => ts.ToDateTimeOffset(),
                dto => Timestamp.FromDateTimeOffset(dto))
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(wd => wd.UpdatedAt)
            .HasConversion(
                ts => ts.ToDateTimeOffset(),
                dto => Timestamp.FromDateTimeOffset(dto))
            .HasColumnName("updated_at")
            .IsRequired();

        builder.Property(wd => wd.IsActive)
            .HasColumnName("is_active")
            .HasDefaultValue(true)
            .IsRequired();

        // EntityBase inherited - ignore columns not in working_days table
        builder.Ignore(wd => wd.DomainEvents);

        // Indexes
        builder.HasIndex(wd => new { wd.TenantId, wd.ProjectId })
            .HasDatabaseName("idx_working_days_tenant_project");

        builder.HasIndex(wd => new { wd.ProjectId, wd.WorkDate })
            .HasDatabaseName("idx_working_days_project_date");

        builder.HasIndex(wd => wd.DayType)
            .HasDatabaseName("idx_working_days_type");

        builder.HasIndex(wd => new { wd.ProjectId, wd.WorkDate })
            .IsUnique()
            .HasDatabaseName("uq_working_days_unique");
    }
}
