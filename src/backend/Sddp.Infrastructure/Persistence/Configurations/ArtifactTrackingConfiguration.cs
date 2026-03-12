using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sddp.Abstractions.ValueObjects;
using Sddp.Domain.Entities;
using Sddp.Domain.Enums;

namespace Sddp.Infrastructure.Persistence.Configurations;

/// <summary>
/// ArtifactTracking EF Core
/// </summary>
public class ArtifactTrackingConfiguration : IEntityTypeConfiguration<ArtifactTracking>
{
    public void Configure(EntityTypeBuilder<ArtifactTracking> builder)
    {
        builder.ToTable("artifact_trackings");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("id")
            .HasConversion(
                v => v.ToGuid(),
                v => GlobalUniqueId.FromGuid(v));

        builder.Property(x => x.TenantId)
            .HasColumnName("tenant_id")
            .IsRequired()
            .HasConversion(
                v => v.ToGuid(),
                v => GlobalUniqueId.FromGuid(v));

        builder.Property(x => x.ProjectId)
            .HasColumnName("project_id")
            .IsRequired()
            .HasConversion(
                v => v.ToGuid(),
                v => GlobalUniqueId.FromGuid(v));

        builder.Property(x => x.SpecId)
            .HasColumnName("spec_id")
            .IsRequired()
            .HasConversion(
                v => v.ToGuid(),
                v => GlobalUniqueId.FromGuid(v));

        builder.Property(x => x.ArtifactPath)
            .HasColumnName("artifact_path")
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(x => x.ArtifactType)
            .HasColumnName("artifact_type")
            .IsRequired()
            .HasConversion<string>();

        builder.Property(x => x.ContentHash)
            .HasColumnName("content_hash")
            .IsRequired()
            .HasMaxLength(128);

        builder.Property(x => x.GeneratorVersion)
            .HasColumnName("generator_version")
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(x => x.TemplateVersion)
            .HasColumnName("template_version")
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(x => x.ArtifactCategory)
            .HasColumnName("artifact_category")
            .IsRequired()
            .HasMaxLength(20)
            .HasDefaultValue("Code");

        builder.Property(x => x.DbObjectName)
            .HasColumnName("db_object_name")
            .HasMaxLength(100);

        builder.Property(x => x.DbSchema)
            .HasColumnName("db_schema")
            .HasMaxLength(50)
            .HasDefaultValue("public");

        builder.Property(x => x.DependsOn)
            .HasColumnName("depends_on")
            .HasColumnType("jsonb");

        builder.Property(x => x.EntityName)
            .HasColumnName("entity_name")
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(x => x.GlossaryTermId)
            .HasColumnName("glossary_term_id")
            .HasConversion(
                v => v.HasValue ? v.Value.ToGuid() : (Guid?)null,
                v => v.HasValue ? GlobalUniqueId.FromGuid(v.Value) : null);

        builder.Property(x => x.SourceConversationId)
            .HasColumnName("source_conversation_id")
            .HasConversion(
                v => v.HasValue ? v.Value.ToGuid() : (Guid?)null,
                v => v.HasValue ? GlobalUniqueId.FromGuid(v.Value) : null);

        builder.Property(x => x.SourceRequirementId)
            .HasColumnName("source_requirement_id")
            .HasConversion(
                v => v.HasValue ? v.Value.ToGuid() : (Guid?)null,
                v => v.HasValue ? GlobalUniqueId.FromGuid(v.Value) : null);

        builder.Property(x => x.OwnerUserId)
            .HasColumnName("owner_user_id")
            .HasConversion(
                v => v.HasValue ? v.Value.ToGuid() : (Guid?)null,
                v => v.HasValue ? GlobalUniqueId.FromGuid(v.Value) : null);

        builder.Property(x => x.CreatedAt)
            .HasConversion(
                ts => ts.ToDateTimeOffset(),
                dto => Timestamp.FromDateTimeOffset(dto))
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(x => x.UpdatedAt)
            .HasConversion(
                ts => ts.ToDateTimeOffset(),
                dto => Timestamp.FromDateTimeOffset(dto))
            .HasColumnName("updated_at")
            .IsRequired();

        builder.Property(x => x.CreatedBy)
            .HasColumnName("created_by")
            .IsRequired()
            .HasConversion(
                v => v.ToGuid(),
                v => GlobalUniqueId.FromGuid(v));

        builder.Property(x => x.UpdatedBy)
            .HasColumnName("updated_by")
            .IsRequired()
            .HasConversion(
                v => v.ToGuid(),
                v => GlobalUniqueId.FromGuid(v));

        builder.Property(x => x.IsActive)
            .HasColumnName("is_active")
            .IsRequired()
            .HasDefaultValue(true);

        // Indexes
        builder.HasIndex(x => new { x.TenantId, x.ProjectId })
            .HasDatabaseName("idx_artifact_tracking_tenant_project");

        builder.HasIndex(x => new { x.TenantId, x.ProjectId, x.SpecId })
            .HasDatabaseName("idx_artifact_tracking_spec");

        builder.HasIndex(x => new { x.TenantId, x.ProjectId, x.ArtifactPath })
            .HasDatabaseName("idx_artifact_tracking_path");

        builder.HasIndex(x => x.EntityName)
            .HasDatabaseName("idx_artifact_tracking_entity_name");

        builder.HasIndex(x => x.GlossaryTermId)
            .HasDatabaseName("idx_artifact_tracking_glossary_term_id");

        builder.HasIndex(x => x.SourceConversationId)
            .HasDatabaseName("idx_artifact_tracking_conversation_id");

        builder.HasIndex(x => x.SourceRequirementId)
            .HasDatabaseName("idx_artifact_tracking_requirement_id");

        builder.HasIndex(x => x.OwnerUserId)
            .HasDatabaseName("idx_artifact_tracking_owner_user_id");

        // Relationships
        builder.HasOne(x => x.Spec)
            .WithMany()
            .HasForeignKey(x => x.SpecId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<GlossaryTerm>()
            .WithMany()
            .HasForeignKey(x => x.GlossaryTermId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne<Conversation>()
            .WithMany()
            .HasForeignKey(x => x.SourceConversationId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne<Requirement>()
            .WithMany()
            .HasForeignKey(x => x.SourceRequirementId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(x => x.OwnerUserId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
