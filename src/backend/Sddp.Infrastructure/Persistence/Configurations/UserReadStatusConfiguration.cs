using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sddp.Abstractions.ValueObjects;
using Sddp.Domain.Entities;

namespace Sddp.Infrastructure.Persistence.Configurations;

public class UserReadStatusConfiguration : IEntityTypeConfiguration<UserReadStatus>
{
    public void Configure(EntityTypeBuilder<UserReadStatus> builder)
    {
        builder.ToTable("user_read_statuses");

        builder.HasKey(rs => rs.Id);

        builder.Property(rs => rs.Id)
            .HasConversion(
                id => id.ToGuid(),
                guid => GlobalUniqueId.FromGuid(guid))
            .HasColumnName("id");

        builder.Property(rs => rs.TenantId)
            .HasConversion(
                id => id.ToGuid(),
                guid => GlobalUniqueId.FromGuid(guid))
            .HasColumnName("tenant_id")
            .IsRequired();

        builder.Property(rs => rs.UserId)
            .HasConversion(
                id => id.ToGuid(),
                guid => GlobalUniqueId.FromGuid(guid))
            .HasColumnName("user_id")
            .IsRequired();

        builder.Property(rs => rs.ConversationId)
            .HasConversion(
                id => id.ToGuid(),
                guid => GlobalUniqueId.FromGuid(guid))
            .HasColumnName("conversation_id")
            .IsRequired();

        builder.Property(rs => rs.LastReadAt)
            .HasConversion(
                ts => ts.HasValue ? ts.Value.ToDateTimeOffset() : (DateTimeOffset?)null,
                dto => dto.HasValue ? Timestamp.FromDateTimeOffset(dto.Value) : null)
            .HasColumnName("last_read_at");

        builder.Property(rs => rs.IsActive)
            .HasColumnName("is_active")
            .HasDefaultValue(true);

        builder.Property(rs => rs.CreatedAt)
            .HasConversion(
                ts => ts.ToDateTimeOffset(),
                dto => Timestamp.FromDateTimeOffset(dto))
            .HasColumnName("created_at");

        builder.Property(rs => rs.UpdatedAt)
            .HasConversion(
                ts => ts.ToDateTimeOffset(),
                dto => Timestamp.FromDateTimeOffset(dto))
            .HasColumnName("updated_at");

        // (user Conversation read status)
        builder.HasIndex(rs => new { rs.UserId, rs.ConversationId }).IsUnique();
        builder.HasIndex(rs => rs.UserId);
        builder.HasIndex(rs => rs.ConversationId);
    }
}
