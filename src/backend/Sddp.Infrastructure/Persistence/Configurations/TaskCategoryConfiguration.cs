using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sddp.Abstractions.ValueObjects;
using Sddp.Domain.Entities;

namespace Sddp.Infrastructure.Persistence.Configurations;

public class TaskCategoryConfiguration : IEntityTypeConfiguration<TaskCategory>
{
    public void Configure(EntityTypeBuilder<TaskCategory> builder)
    {
        builder.ToTable("task_categories");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.Id)
            .HasConversion(
                id => id.ToGuid(),
                guid => GlobalUniqueId.FromGuid(guid))
            .HasColumnName("id");

        builder.Property(c => c.TenantId)
            .HasConversion(
                id => id.ToGuid(),
                guid => GlobalUniqueId.FromGuid(guid))
            .HasColumnName("tenant_id")
            .IsRequired();

        builder.Property(c => c.UserId)
            .HasConversion(
                id => id.ToGuid(),
                guid => GlobalUniqueId.FromGuid(guid))
            .HasColumnName("user_id")
            .IsRequired();

        builder.Property(c => c.Name)
            .HasColumnName("name")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(c => c.Icon)
            .HasColumnName("icon")
            .HasMaxLength(50);

        builder.Property(c => c.SortOrder)
            .HasColumnName("sort_order")
            .HasDefaultValue(0);

        // EntityBase
        builder.Property(c => c.IsActive)
            .HasColumnName("is_active")
            .HasDefaultValue(true);

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

        //
        builder.HasIndex(c => new { c.TenantId, c.UserId })
            .HasDatabaseName("ix_task_categories_tenant_user");

        builder.HasIndex(c => new { c.TenantId, c.UserId, c.Name })
            .IsUnique()
            .HasFilter("is_active = TRUE")
            .HasDatabaseName("uq_task_categories_name");
    }
}
