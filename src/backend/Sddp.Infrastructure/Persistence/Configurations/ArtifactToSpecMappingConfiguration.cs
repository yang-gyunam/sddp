using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sddp.Abstractions.ValueObjects;
using Sddp.Domain.Entities;
using Sddp.Domain.Enums;

namespace Sddp.Infrastructure.Persistence.Configurations;

/// <summary>
/// ArtifactToSpecMapping EF Core
/// </summary>
public class ArtifactToSpecMappingConfiguration : IEntityTypeConfiguration<ArtifactToSpecMapping>
{
    public void Configure(EntityTypeBuilder<ArtifactToSpecMapping> builder)
    {
        builder.ToTable("artifact_to_spec_mappings");

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

        builder.Property(x => x.MappingReason)
            .HasColumnName("mapping_reason")
            .IsRequired()
            .HasConversion<string>();

        builder.Property(x => x.SourceContent)
            .HasColumnName("source_content");

        builder.Property(x => x.Notes)
            .HasColumnName("notes");

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
            .HasDatabaseName("idx_asm_tenant_project");

        builder.HasIndex(x => x.SpecId)
            .HasDatabaseName("idx_asm_spec_id");

        builder.HasIndex(x => new { x.TenantId, x.ProjectId, x.ArtifactPath })
            .HasDatabaseName("idx_asm_artifact_path");

        // Unique constraint
        builder.HasIndex(x => new { x.TenantId, x.ProjectId, x.SpecId, x.ArtifactPath })
            .IsUnique()
            .HasDatabaseName("uq_asm_tenant_project_spec_path");

        // Relationships
        builder.HasOne(x => x.Spec)
            .WithMany()
            .HasForeignKey(x => x.SpecId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
