using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sddp.Abstractions.ValueObjects;
using Sddp.Domain.Entities;

namespace Sddp.Infrastructure.Persistence.Configurations;

public class EntityMetadataConfiguration : IEntityTypeConfiguration<EntityMetadata>
{
    public void Configure(EntityTypeBuilder<EntityMetadata> builder)
    {
        builder.ToTable("entity_metadata");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
            .HasConversion(
                id => id.ToGuid(),
                guid => GlobalUniqueId.FromGuid(guid))
            .HasColumnName("id");

        builder.Property(e => e.TenantId)
            .HasConversion(
                id => id.ToGuid(),
                guid => GlobalUniqueId.FromGuid(guid))
            .HasColumnName("tenant_id")
            .IsRequired();

        builder.Property(e => e.ProjectId)
            .HasConversion(
                id => id.ToGuid(),
                guid => GlobalUniqueId.FromGuid(guid))
            .HasColumnName("project_id")
            .IsRequired();

        builder.Property(e => e.SpecId)
            .HasConversion(
                id => id.ToGuid(),
                guid => GlobalUniqueId.FromGuid(guid))
            .HasColumnName("spec_id")
            .IsRequired();

        builder.Property(e => e.EntityName)
            .HasColumnName("entity_name")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(e => e.TableName)
            .HasColumnName("table_name")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(e => e.Namespace)
            .HasColumnName("namespace")
            .HasMaxLength(500)
            .IsRequired()
            .HasDefaultValue("Sddp.Domain.Entities");

        builder.Property(e => e.Description)
            .HasColumnName("description")
            .HasColumnType("text");

        builder.Property(e => e.BaseClass)
            .HasColumnName("base_class")
            .HasMaxLength(200)
            .IsRequired()
            .HasDefaultValue("AuditableEntityBase");

        builder.Property(e => e.IsGenerated)
            .HasColumnName("is_generated")
            .HasDefaultValue(true);

        // AuditableEntityBase
        builder.Property(e => e.CreatedBy)
            .HasConversion(
                id => id.ToGuid(),
                guid => GlobalUniqueId.FromGuid(guid))
            .HasColumnName("created_by");

        builder.Property(e => e.UpdatedBy)
            .HasConversion(
                id => id.ToGuid(),
                guid => GlobalUniqueId.FromGuid(guid))
            .HasColumnName("updated_by");

        // EntityBase
        builder.Property(e => e.IsActive)
            .HasColumnName("is_active")
            .HasDefaultValue(true);

        builder.Property(e => e.CreatedAt)
            .HasConversion(
                ts => ts.ToDateTimeOffset(),
                dto => Timestamp.FromDateTimeOffset(dto))
            .HasColumnName("created_at");

        builder.Property(e => e.UpdatedAt)
            .HasConversion(
                ts => ts.ToDateTimeOffset(),
                dto => Timestamp.FromDateTimeOffset(dto))
            .HasColumnName("updated_at");

        // relationship: Spec
        builder.HasOne(e => e.Spec)
            .WithMany()
            .HasForeignKey(e => e.SpecId)
            .OnDelete(DeleteBehavior.Restrict);

        //
        builder.HasIndex(e => new { e.TenantId, e.ProjectId, e.EntityName })
            .IsUnique()
            .HasDatabaseName("ix_entity_metadata_tenant_project_entity");

        builder.HasIndex(e => e.SpecId)
            .HasDatabaseName("ix_entity_metadata_spec_id");

        builder.HasIndex(e => new { e.TenantId, e.ProjectId })
            .HasDatabaseName("ix_entity_metadata_tenant_project");

        builder.HasIndex(e => e.IsGenerated)
            .HasDatabaseName("ix_entity_metadata_is_generated");
    }
}
