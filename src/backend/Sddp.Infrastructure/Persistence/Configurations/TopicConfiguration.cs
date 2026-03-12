using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sddp.Abstractions.Constants;
using Sddp.Abstractions.ValueObjects;
using Sddp.Domain.Entities;

namespace Sddp.Infrastructure.Persistence.Configurations;

public class TopicConfiguration : IEntityTypeConfiguration<Topic>
{
    public void Configure(EntityTypeBuilder<Topic> builder)
    {
        builder.ToTable("topics");

        builder.HasKey(t => t.Id);

        builder.Property(t => t.Id)
            .HasConversion(
                id => id.ToGuid(),
                guid => GlobalUniqueId.FromGuid(guid))
            .HasColumnName("id");

        builder.Property(t => t.ForumId)
            .HasConversion(
                id => id.ToGuid(),
                guid => GlobalUniqueId.FromGuid(guid))
            .HasColumnName("forum_id")
            .IsRequired();

        builder.Property(t => t.Title)
            .HasColumnName("title")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(t => t.AuthorId)
            .HasConversion(
                id => id.ToGuid(),
                guid => GlobalUniqueId.FromGuid(guid))
            .HasColumnName("author_id")
            .IsRequired();

        builder.Property(t => t.Status)
            .HasColumnName("status_code_id")
            .HasConversion(
                e => WellKnownCodes.TopicStatusToId[e],
                id => WellKnownCodes.IdToTopicStatus[id])
            .IsRequired();

        builder.Property(t => t.IsPinned)
            .HasColumnName("is_pinned")
            .HasDefaultValue(false);

        builder.Property(t => t.IsLocked)
            .HasColumnName("is_locked")
            .HasDefaultValue(false);

        builder.Property(t => t.DecisionSpecId)
            .HasConversion(
                id => id.HasValue ? id.Value.ToGuid() : (Guid?)null,
                guid => guid.HasValue ? GlobalUniqueId.FromGuid(guid.Value) : null)
            .HasColumnName("decision_spec_id");

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

        //
        builder.HasIndex(t => t.ForumId);
        builder.HasIndex(t => t.AuthorId);
        builder.HasIndex(t => t.Status);
        builder.HasIndex(t => t.IsActive);

        // relationship - Forum (ForumConfiguration HasMany settings)

        // relationship - Author (User)
        builder.HasOne(t => t.Author)
            .WithMany()
            .HasForeignKey(t => t.AuthorId)
            .OnDelete(DeleteBehavior.Restrict);

        // relationship - Messages
        builder.HasMany(t => t.Messages)
            .WithOne(m => m.Topic)
            .HasForeignKey(m => m.TopicId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
