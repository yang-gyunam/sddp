using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sddp.Abstractions.ValueObjects;
using Sddp.Domain.Entities;

namespace Sddp.Infrastructure.Persistence.Configurations;

public class SlaPolicyConfiguration : IEntityTypeConfiguration<SlaPolicy>
{
    public void Configure(EntityTypeBuilder<SlaPolicy> builder)
    {
        builder.ToTable("sla_policies");

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

        builder.Property(e => e.SlaType)
            .HasColumnName("sla_type")
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(e => e.SlaHours)
            .HasColumnName("sla_hours")
            .IsRequired();

        builder.Property(e => e.UrgentSlaHours)
            .HasColumnName("urgent_sla_hours")
            .IsRequired();

        builder.Property(e => e.ReminderAtPercent)
            .HasColumnName("reminder_at_percent")
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(e => e.EscalationRole)
            .HasColumnName("escalation_role")
            .HasMaxLength(50);

        // EntityBase
        builder.Property(e => e.IsActive)
            .HasColumnName("is_active")
            .HasDefaultValue(true);

        builder.Property(e => e.CreatedAt)
            .HasConversion(
                ts => ts.ToDateTimeOffset(),
                dto => Timestamp.FromDateTimeOffset(dto))
            .HasColumnName("created_at");

        builder.Property(e => e.UpdatedAt)
            .HasConversion(
                ts => ts.ToDateTimeOffset(),
                dto => Timestamp.FromDateTimeOffset(dto))
            .HasColumnName("updated_at");

        //
        builder.HasIndex(e => new { e.TenantId, e.ProjectId })
            .HasDatabaseName("idx_slap_tenant_project");

        builder.HasIndex(e => new { e.TenantId, e.ProjectId, e.SlaType })
            .IsUnique()
            .HasDatabaseName("uq_slap_project_type");
    }
}
