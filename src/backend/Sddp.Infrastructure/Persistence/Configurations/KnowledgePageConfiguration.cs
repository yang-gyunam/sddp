using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sddp.Abstractions.ValueObjects;
using Sddp.Domain.Entities;

namespace Sddp.Infrastructure.Persistence.Configurations;

public sealed class KnowledgePageConfiguration : IEntityTypeConfiguration<KnowledgePage>
{
    public void Configure(EntityTypeBuilder<KnowledgePage> builder)
    {
        builder.ToTable("knowledge_pages");
        builder.HasKey(page => page.Id);

        ConfigureId(builder.Property(page => page.Id), "id");
        ConfigureId(builder.Property(page => page.TenantId), "tenant_id");
        ConfigureId(builder.Property(page => page.ProjectId), "project_id");
        ConfigureNullableId(builder.Property(page => page.ParentId), "parent_id");
        ConfigureId(builder.Property(page => page.CreatedBy), "created_by");
        ConfigureId(builder.Property(page => page.UpdatedBy), "updated_by");
        ConfigureNullableId(builder.Property(page => page.PublishedBy), "published_by");

        builder.Property(page => page.Title)
            .HasColumnName("title")
            .HasMaxLength(200)
            .IsRequired();
        builder.Property(page => page.Slug)
            .HasColumnName("slug")
            .HasMaxLength(200)
            .IsRequired();
        builder.Property(page => page.ContentJson)
            .HasColumnName("content_json")
            .HasColumnType("jsonb")
            .IsRequired();
        builder.Property(page => page.ContentText)
            .HasColumnName("content_text")
            .IsRequired();
        builder.Property(page => page.TagsJson)
            .HasColumnName("tags_json")
            .HasColumnType("jsonb")
            .IsRequired();
        builder.Property(page => page.Status)
            .HasConversion<string>()
            .HasColumnName("status")
            .HasMaxLength(20)
            .IsRequired();
        builder.Property(page => page.Position)
            .HasColumnName("position")
            .IsRequired();
        builder.Property(page => page.Revision)
            .HasColumnName("revision")
            .IsConcurrencyToken()
            .IsRequired();

        ConfigureNullableTimestamp(builder.Property(page => page.PublishedAt), "published_at");
        ConfigureNullableTimestamp(builder.Property(page => page.ArchivedAt), "archived_at");
        ConfigureTimestamp(builder.Property(page => page.CreatedAt), "created_at");
        ConfigureTimestamp(builder.Property(page => page.UpdatedAt), "updated_at");
        builder.Property(page => page.IsActive)
            .HasColumnName("is_active")
            .HasDefaultValue(true)
            .IsRequired();

        // Composite scope FK names must match scripts/db/26-schema-knowledge.sql and
        // scripts/db/migrations/V006__enforce_knowledge_scope.sql, which are the schema
        // source of truth and are asserted by the database integrity gate.
        builder.HasOne(page => page.Parent)
            .WithMany(page => page.Children)
            .HasForeignKey(page => new { page.TenantId, page.ProjectId, page.ParentId })
            .HasPrincipalKey(page => new { page.TenantId, page.ProjectId, page.Id })
            .HasConstraintName("fk_knowledge_pages_parent_scope")
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasMany(page => page.Versions)
            .WithOne(version => version.Page)
            .HasForeignKey(version => new { version.TenantId, version.ProjectId, version.PageId })
            .HasPrincipalKey(page => new { page.TenantId, page.ProjectId, page.Id })
            .HasConstraintName("fk_knowledge_page_versions_page_scope")
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasMany(page => page.Links)
            .WithOne(link => link.Page)
            .HasForeignKey(link => new { link.TenantId, link.ProjectId, link.PageId })
            .HasPrincipalKey(page => new { page.TenantId, page.ProjectId, page.Id })
            .HasConstraintName("fk_knowledge_page_links_page_scope")
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Project>()
            .WithMany()
            .HasForeignKey(page => new { page.TenantId, page.ProjectId })
            .HasPrincipalKey(project => new { project.TenantId, project.Id })
            .HasConstraintName("fk_knowledge_pages_project_scope")
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasAlternateKey(page => new { page.TenantId, page.ProjectId, page.Id })
            .HasName("uq_knowledge_pages_scope_id");
        builder.HasIndex(page => new { page.TenantId, page.ProjectId, page.ParentId, page.Position })
            .HasDatabaseName("idx_knowledge_pages_tree");
        builder.HasIndex(page => new { page.TenantId, page.ProjectId, page.Status, page.UpdatedAt })
            .HasDatabaseName("idx_knowledge_pages_status_updated");
    }

    private static void ConfigureId(
        PropertyBuilder<GlobalUniqueId> property,
        string columnName) =>
        property
            .HasConversion(id => id.ToGuid(), value => GlobalUniqueId.FromGuid(value))
            .HasColumnName(columnName)
            .IsRequired();

    private static void ConfigureNullableId(
        PropertyBuilder<GlobalUniqueId?> property,
        string columnName) =>
        property
            .HasConversion(
                id => id.HasValue ? id.Value.ToGuid() : (Guid?)null,
                value => value.HasValue ? GlobalUniqueId.FromGuid(value.Value) : null)
            .HasColumnName(columnName);

    private static void ConfigureTimestamp(
        PropertyBuilder<Timestamp> property,
        string columnName) =>
        property
            .HasConversion(value => value.ToDateTimeOffset(), value => Timestamp.FromDateTimeOffset(value))
            .HasColumnName(columnName)
            .IsRequired();

    private static void ConfigureNullableTimestamp(
        PropertyBuilder<Timestamp?> property,
        string columnName) =>
        property
            .HasConversion(
                value => value.HasValue ? value.Value.ToDateTimeOffset() : (DateTimeOffset?)null,
                value => value.HasValue ? Timestamp.FromDateTimeOffset(value.Value) : null)
            .HasColumnName(columnName);
}
