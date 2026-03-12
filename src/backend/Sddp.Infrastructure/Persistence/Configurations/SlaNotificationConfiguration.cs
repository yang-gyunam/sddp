using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sddp.Abstractions.ValueObjects;
using Sddp.Domain.Entities;

namespace Sddp.Infrastructure.Persistence.Configurations;

public class SlaNotificationConfiguration : IEntityTypeConfiguration<SlaNotification>
{
    public void Configure(EntityTypeBuilder<SlaNotification> builder)
    {
        builder.ToTable("sla_notifications");

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

        builder.Property(e => e.SignOffId)
            .HasConversion(
                id => id.ToGuid(),
                guid => GlobalUniqueId.FromGuid(guid))
            .HasColumnName("sign_off_id")
            .IsRequired();

        builder.Property(e => e.ThresholdPercent)
            .HasColumnName("threshold_percent")
            .IsRequired();

        builder.Property(e => e.NotificationType)
            .HasColumnName("notification_type")
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(e => e.NotifiedAt)
            .HasConversion(
                ts => ts.ToDateTimeOffset(),
                dto => Timestamp.FromDateTimeOffset(dto))
            .HasColumnName("notified_at")
            .IsRequired();

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

        // relationship: SignOff
        builder.HasOne(e => e.SignOff)
            .WithMany()
            .HasForeignKey(e => e.SignOffId)
            .OnDelete(DeleteBehavior.Cascade);

        //
        builder.HasIndex(e => e.SignOffId)
            .HasDatabaseName("idx_slan_signoff_id");

        builder.HasIndex(e => new { e.TenantId, e.ProjectId })
            .HasDatabaseName("idx_slan_tenant_project");

        builder.HasIndex(e => new { e.SignOffId, e.ThresholdPercent })
            .IsUnique()
            .HasDatabaseName("uq_slan_signoff_threshold");
    }
}
