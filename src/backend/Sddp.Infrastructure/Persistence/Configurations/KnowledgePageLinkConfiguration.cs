using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sddp.Abstractions.ValueObjects;
using Sddp.Domain.Entities;

namespace Sddp.Infrastructure.Persistence.Configurations;

public sealed class KnowledgePageLinkConfiguration : IEntityTypeConfiguration<KnowledgePageLink>
{
    public void Configure(EntityTypeBuilder<KnowledgePageLink> builder)
    {
        builder.ToTable("knowledge_page_links");
        builder.HasKey(link => link.Id);

        ConfigureId(builder.Property(link => link.Id), "id");
        ConfigureId(builder.Property(link => link.TenantId), "tenant_id");
        ConfigureId(builder.Property(link => link.ProjectId), "project_id");
        ConfigureId(builder.Property(link => link.PageId), "page_id");
        ConfigureId(builder.Property(link => link.TargetId), "target_id");

        builder.Property(link => link.TargetType)
            .HasConversion<string>()
            .HasColumnName("target_type")
            .HasMaxLength(40)
            .IsRequired();
        builder.Property(link => link.CreatedAt)
            .HasConversion(value => value.ToDateTimeOffset(), value => Timestamp.FromDateTimeOffset(value))
            .HasColumnName("created_at")
            .IsRequired();
        builder.Property(link => link.UpdatedAt)
            .HasConversion(value => value.ToDateTimeOffset(), value => Timestamp.FromDateTimeOffset(value))
            .HasColumnName("updated_at")
            .IsRequired();
        builder.Property(link => link.IsActive)
            .HasColumnName("is_active")
            .HasDefaultValue(true)
            .IsRequired();

        builder.HasIndex(link => new { link.PageId, link.TargetType, link.TargetId })
            .IsUnique()
            .HasFilter("is_active = TRUE")
            .HasDatabaseName("uq_knowledge_page_links_active");
        builder.HasIndex(link => new { link.TenantId, link.ProjectId, link.TargetType, link.TargetId })
            .HasDatabaseName("idx_knowledge_page_links_backlink");
    }

    private static void ConfigureId(
        PropertyBuilder<GlobalUniqueId> property,
        string columnName) =>
        property
            .HasConversion(id => id.ToGuid(), value => GlobalUniqueId.FromGuid(value))
            .HasColumnName(columnName)
            .IsRequired();
}
