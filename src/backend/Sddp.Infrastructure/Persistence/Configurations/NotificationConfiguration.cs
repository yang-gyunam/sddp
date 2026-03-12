using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sddp.Abstractions.ValueObjects;
using Sddp.Domain.Entities;

namespace Sddp.Infrastructure.Persistence.Configurations;

public class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> builder)
    {
        builder.ToTable("notifications");

        builder.HasKey(n => n.Id);

        builder.Property(n => n.Id)
            .HasConversion(
                id => id.ToGuid(),
                guid => GlobalUniqueId.FromGuid(guid))
            .HasColumnName("id");

        builder.Property(n => n.TenantId)
            .HasConversion(
                id => id.ToGuid(),
                guid => GlobalUniqueId.FromGuid(guid))
            .HasColumnName("tenant_id")
            .IsRequired();

        builder.Property(n => n.RecipientId)
            .HasConversion(
                id => id.ToGuid(),
                guid => GlobalUniqueId.FromGuid(guid))
            .HasColumnName("recipient_id")
            .IsRequired();

        builder.Property(n => n.ActorId)
            .HasConversion(
                id => id.HasValue ? id.Value.ToGuid() : (Guid?)null,
                guid => guid.HasValue ? GlobalUniqueId.FromGuid(guid.Value) : null)
            .HasColumnName("actor_id");

        builder.Property(n => n.Type)
            .HasColumnName("type")
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(n => n.Title)
            .HasColumnName("title")
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(n => n.Message)
            .HasColumnName("message")
            .HasColumnType("text");

        builder.Property(n => n.EntityType)
            .HasColumnName("entity_type")
            .HasMaxLength(50);

        builder.Property(n => n.EntityId)
            .HasConversion(
                id => id.HasValue ? id.Value.ToGuid() : (Guid?)null,
                guid => guid.HasValue ? GlobalUniqueId.FromGuid(guid.Value) : null)
            .HasColumnName("entity_id");

        builder.Property(n => n.IsRead)
            .HasColumnName("is_read")
            .HasDefaultValue(false)
            .IsRequired();

        builder.Property(n => n.ReadAt)
            .HasConversion(
                ts => ts.HasValue ? ts.Value.ToDateTimeOffset() : (DateTimeOffset?)null,
                dto => dto.HasValue ? Timestamp.FromDateTimeOffset(dto.Value) : null)
            .HasColumnName("read_at");

        // EntityBase
        builder.Property(n => n.IsActive)
            .HasColumnName("is_active")
            .HasDefaultValue(true)
            .IsRequired();

        builder.Property(n => n.CreatedAt)
            .HasConversion(
                ts => ts.ToDateTimeOffset(),
                dto => Timestamp.FromDateTimeOffset(dto))
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(n => n.UpdatedAt)
            .HasConversion(
                ts => ts.ToDateTimeOffset(),
                dto => Timestamp.FromDateTimeOffset(dto))
            .HasColumnName("updated_at")
            .IsRequired();

        builder.Ignore(n => n.DomainEvents);

        //
        builder.HasIndex(n => new { n.TenantId, n.RecipientId })
            .HasDatabaseName("idx_notifications_tenant_recipient");

        builder.HasIndex(n => new { n.RecipientId, n.IsRead, n.CreatedAt })
            .HasDatabaseName("idx_notifications_recipient_unread");

        builder.HasIndex(n => n.CreatedAt)
            .HasDatabaseName("idx_notifications_created_at");
    }
}
