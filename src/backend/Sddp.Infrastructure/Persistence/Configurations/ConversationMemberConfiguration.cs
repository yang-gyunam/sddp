using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sddp.Abstractions.ValueObjects;
using Sddp.Domain.Entities;

namespace Sddp.Infrastructure.Persistence.Configurations;

public class ConversationMemberConfiguration : IEntityTypeConfiguration<ConversationMember>
{
    public void Configure(EntityTypeBuilder<ConversationMember> builder)
    {
        builder.ToTable("conversation_members");

        builder.HasKey(m => m.Id);

        builder.Property(m => m.Id)
            .HasConversion(
                id => id.ToGuid(),
                guid => GlobalUniqueId.FromGuid(guid))
            .HasColumnName("id");

        builder.Property(m => m.ConversationId)
            .HasConversion(
                id => id.ToGuid(),
                guid => GlobalUniqueId.FromGuid(guid))
            .HasColumnName("conversation_id")
            .IsRequired();

        builder.Property(m => m.UserId)
            .HasConversion(
                id => id.ToGuid(),
                guid => GlobalUniqueId.FromGuid(guid))
            .HasColumnName("user_id")
            .IsRequired();

        builder.Property(m => m.Role)
            .HasColumnName("role")
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(m => m.Type)
            .HasColumnName("member_type")
            .HasConversion<string>()
            .HasMaxLength(10)
            .IsRequired();

        builder.Property(m => m.JoinedAt)
            .HasConversion(
                ts => ts.ToDateTimeOffset(),
                dto => Timestamp.FromDateTimeOffset(dto))
            .HasColumnName("joined_at")
            .IsRequired();

        builder.Property(m => m.MutedUntil)
            .HasConversion(
                ts => ts.HasValue ? ts.Value.ToDateTimeOffset() : (DateTimeOffset?)null,
                dto => dto.HasValue ? Timestamp.FromDateTimeOffset(dto.Value) : null)
            .HasColumnName("muted_until");

        builder.Property(m => m.NotificationsEnabled)
            .HasColumnName("notifications_enabled")
            .HasDefaultValue(true);

        builder.Property(m => m.IsActive)
            .HasColumnName("is_active")
            .HasDefaultValue(true);

        // conversation_members created_at, updated_at
        builder.Ignore(m => m.CreatedAt);
        builder.Ignore(m => m.UpdatedAt);

        //
        builder.HasIndex(m => new { m.ConversationId, m.UserId })
            .IsUnique();
        builder.HasIndex(m => m.ConversationId);
        builder.HasIndex(m => m.UserId);
        builder.HasIndex(m => m.IsActive);

        // relationship - User
        builder.HasOne(m => m.User)
            .WithMany()
            .HasForeignKey(m => m.UserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
