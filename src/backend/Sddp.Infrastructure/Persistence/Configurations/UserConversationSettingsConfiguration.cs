using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sddp.Abstractions.ValueObjects;
using Sddp.Domain.Entities;

namespace Sddp.Infrastructure.Persistence.Configurations;

public class UserConversationSettingsConfiguration : IEntityTypeConfiguration<UserConversationSettings>
{
    public void Configure(EntityTypeBuilder<UserConversationSettings> builder)
    {
        builder.ToTable("user_conversation_settings");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.Id)
            .HasConversion(
                id => id.ToGuid(),
                guid => GlobalUniqueId.FromGuid(guid))
            .HasColumnName("id");

        builder.Property(s => s.TenantId)
            .HasConversion(
                id => id.ToGuid(),
                guid => GlobalUniqueId.FromGuid(guid))
            .HasColumnName("tenant_id")
            .IsRequired();

        builder.Property(s => s.UserId)
            .HasConversion(
                id => id.ToGuid(),
                guid => GlobalUniqueId.FromGuid(guid))
            .HasColumnName("user_id")
            .IsRequired();

        builder.Property(s => s.ConversationId)
            .HasConversion(
                id => id.ToGuid(),
                guid => GlobalUniqueId.FromGuid(guid))
            .HasColumnName("conversation_id")
            .IsRequired();

        builder.Property(s => s.IsStarred)
            .HasColumnName("is_starred")
            .HasDefaultValue(false);

        builder.Property(s => s.IsMuted)
            .HasColumnName("is_muted")
            .HasDefaultValue(false);

        builder.Property(s => s.IsActive)
            .HasColumnName("is_active")
            .HasDefaultValue(true);

        builder.Property(s => s.CreatedAt)
            .HasConversion(
                ts => ts.ToDateTimeOffset(),
                dto => Timestamp.FromDateTimeOffset(dto))
            .HasColumnName("created_at");

        builder.Property(s => s.UpdatedAt)
            .HasConversion(
                ts => ts.ToDateTimeOffset(),
                dto => Timestamp.FromDateTimeOffset(dto))
            .HasColumnName("updated_at");

        // (user Conversation settings)
        builder.HasIndex(s => new { s.UserId, s.ConversationId }).IsUnique();
        builder.HasIndex(s => s.UserId);
        builder.HasIndex(s => s.ConversationId);
        builder.HasIndex(s => s.IsStarred);
    }
}
