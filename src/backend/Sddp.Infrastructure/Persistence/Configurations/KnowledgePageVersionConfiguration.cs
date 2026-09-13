using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sddp.Abstractions.ValueObjects;
using Sddp.Domain.Entities;

namespace Sddp.Infrastructure.Persistence.Configurations;

public sealed class KnowledgePageVersionConfiguration : IEntityTypeConfiguration<KnowledgePageVersion>
{
    public void Configure(EntityTypeBuilder<KnowledgePageVersion> builder)
    {
        builder.ToTable("knowledge_page_versions");
        builder.HasKey(version => version.Id);

        ConfigureId(builder.Property(version => version.Id), "id");
        ConfigureId(builder.Property(version => version.TenantId), "tenant_id");
        ConfigureId(builder.Property(version => version.ProjectId), "project_id");
        ConfigureId(builder.Property(version => version.PageId), "page_id");
        ConfigureId(builder.Property(version => version.CreatedBy), "created_by");

        builder.Property(version => version.Revision)
            .HasColumnName("revision")
            .IsRequired();
        builder.Property(version => version.Title)
            .HasColumnName("title")
            .HasMaxLength(200)
            .IsRequired();
        builder.Property(version => version.Slug)
            .HasColumnName("slug")
            .HasMaxLength(200)
            .IsRequired();
        builder.Property(version => version.ContentJson)
            .HasColumnName("content_json")
            .HasColumnType("jsonb")
            .IsRequired();
        builder.Property(version => version.ContentText)
            .HasColumnName("content_text")
            .IsRequired();
        builder.Property(version => version.TagsJson)
            .HasColumnName("tags_json")
            .HasColumnType("jsonb")
            .IsRequired();
        builder.Property(version => version.Status)
            .HasConversion<string>()
            .HasColumnName("status")
            .HasMaxLength(20)
            .IsRequired();
        builder.Property(version => version.ChangeSummary)
            .HasColumnName("change_summary")
            .HasMaxLength(500);
        builder.Property(version => version.CreatedAt)
            .HasConversion(value => value.ToDateTimeOffset(), value => Timestamp.FromDateTimeOffset(value))
            .HasColumnName("created_at")
            .IsRequired();
        builder.Property(version => version.UpdatedAt)
            .HasConversion(value => value.ToDateTimeOffset(), value => Timestamp.FromDateTimeOffset(value))
            .HasColumnName("updated_at")
            .IsRequired();
        builder.Property(version => version.IsActive)
            .HasColumnName("is_active")
            .HasDefaultValue(true)
            .IsRequired();

        builder.HasIndex(version => new { version.PageId, version.Revision })
            .IsUnique()
            .HasDatabaseName("uq_knowledge_page_versions_revision");
        builder.HasIndex(version => new { version.TenantId, version.ProjectId, version.PageId })
            .HasDatabaseName("idx_knowledge_page_versions_scope");
    }

    private static void ConfigureId(
        PropertyBuilder<GlobalUniqueId> property,
        string columnName) =>
        property
            .HasConversion(id => id.ToGuid(), value => GlobalUniqueId.FromGuid(value))
            .HasColumnName(columnName)
            .IsRequired();
}
