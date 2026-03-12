using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sddp.Abstractions.Enums;
using Sddp.Abstractions.ValueObjects;
using Sddp.Domain.Entities;

namespace Sddp.Infrastructure.Persistence.Configurations;

public class ConversationConfiguration : IEntityTypeConfiguration<Conversation>
{
    public void Configure(EntityTypeBuilder<Conversation> builder)
    {
        builder.ToTable("conversations");

        // TPT
        builder.UseTptMappingStrategy();

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

        builder.Property(c => c.ProjectId)
            .HasConversion(
                id => id.HasValue ? id.Value.ToGuid() : (Guid?)null,
                guid => guid.HasValue ? GlobalUniqueId.FromGuid(guid.Value) : null)
            .HasColumnName("project_id");

        builder.Property(c => c.Name)
            .HasColumnName("name")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(c => c.Description)
            .HasColumnName("description")
            .HasMaxLength(1000);

        builder.Property(c => c.ConversationType)
            .HasColumnName("conversation_type")
            .HasMaxLength(20)
            .HasConversion(
                value => value.ToString(),
                value => Enum.Parse<ConversationType>(value))
            .IsRequired();

        builder.Property(c => c.Visibility)
            .HasColumnName("visibility")
            .HasMaxLength(20)
            .HasConversion(
                value => value.ToString(),
                value => Enum.Parse<ConversationVisibility>(value))
            .IsRequired();

        builder.Property(c => c.Scope)
            .HasColumnName("scope")
            .HasMaxLength(20)
            .HasConversion(
                value => value.ToString(),
                value => Enum.Parse<ConversationScope>(value))
            .IsRequired();

        builder.Property(c => c.SortOrder)
            .HasColumnName("sort_order")
            .HasDefaultValue(0);

        builder.Property(c => c.IsArchived)
            .HasColumnName("is_archived")
            .HasDefaultValue(false);

        builder.Property(c => c.IsDefault)
            .HasColumnName("is_default")
            .HasDefaultValue(false);

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
        builder.HasIndex(c => new { c.TenantId, c.ProjectId });
        builder.HasIndex(c => c.TenantId)
            .HasFilter("project_id IS NULL")
            .HasDatabaseName("ix_conversations_tenant_id_global");
        builder.HasIndex(c => c.ConversationType);
        builder.HasIndex(c => c.Visibility);
        builder.HasIndex(c => new { c.TenantId, c.Scope, c.ConversationType })
            .HasDatabaseName("idx_conversations_taxonomy_scope_type");
        builder.HasIndex(c => c.IsActive);
        builder.HasIndex(c => c.IsArchived);

        // relationship - Messages (Conversation message)
        builder.HasMany(c => c.Messages)
            .WithOne(m => m.Conversation)
            .HasForeignKey(m => m.ConversationId)
            .OnDelete(DeleteBehavior.Cascade);

        // relationship - Members
        builder.HasMany(c => c.Members)
            .WithOne(m => m.Conversation)
            .HasForeignKey(m => m.ConversationId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
