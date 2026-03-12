using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sddp.Abstractions.ValueObjects;
using Sddp.Domain.Entities;
using Sddp.Domain.Enums;

namespace Sddp.Infrastructure.Persistence.Configurations;

public class EntityRelationshipMetadataConfiguration : IEntityTypeConfiguration<EntityRelationshipMetadata>
{
    public void Configure(EntityTypeBuilder<EntityRelationshipMetadata> builder)
    {
        builder.ToTable("entity_relationship_metadata");

        builder.HasKey(r => r.Id);

        builder.Property(r => r.Id)
            .HasConversion(
                id => id.ToGuid(),
                guid => GlobalUniqueId.FromGuid(guid))
            .HasColumnName("id");

        builder.Property(r => r.EntityMetadataId)
            .HasConversion(
                id => id.ToGuid(),
                guid => GlobalUniqueId.FromGuid(guid))
            .HasColumnName("entity_metadata_id")
            .IsRequired();

        builder.Property(r => r.RelationshipType)
            .HasColumnName("relationship_type")
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(r => r.TargetEntity)
            .HasColumnName("target_entity")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(r => r.ForeignKeyName)
            .HasColumnName("foreign_key_name")
            .HasMaxLength(200);

        builder.Property(r => r.ForeignKeyColumn)
            .HasColumnName("foreign_key_column")
            .HasMaxLength(200);

        builder.Property(r => r.NavigationName)
            .HasColumnName("navigation_name")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(r => r.InverseNavigationName)
            .HasColumnName("inverse_navigation_name")
            .HasMaxLength(200);

        builder.Property(r => r.IsRequired)
            .HasColumnName("is_required")
            .HasDefaultValue(false);

        builder.Property(r => r.OnDelete)
            .HasColumnName("on_delete")
            .HasConversion<string>()
            .HasMaxLength(20)
            .HasDefaultValue(RelationshipDeleteBehavior.Restrict);

        builder.Property(r => r.Description)
            .HasColumnName("description")
            .HasColumnType("text");

        builder.Property(r => r.DisplayOrder)
            .HasColumnName("display_order")
            .HasDefaultValue(0);

        // EntityBase
        builder.Property(r => r.IsActive)
            .HasColumnName("is_active")
            .HasDefaultValue(true);

        builder.Property(r => r.CreatedAt)
            .HasConversion(
                ts => ts.ToDateTimeOffset(),
                dto => Timestamp.FromDateTimeOffset(dto))
            .HasColumnName("created_at");

        builder.Property(r => r.UpdatedAt)
            .HasConversion(
                ts => ts.ToDateTimeOffset(),
                dto => Timestamp.FromDateTimeOffset(dto))
            .HasColumnName("updated_at");

        // relationship: EntityMetadata
        builder.HasOne(r => r.EntityMetadata)
            .WithMany(e => e.Relationships)
            .HasForeignKey(r => r.EntityMetadataId)
            .OnDelete(DeleteBehavior.Cascade);

        //
        builder.HasIndex(r => r.EntityMetadataId)
            .HasDatabaseName("ix_entity_relationship_metadata_entity_metadata_id");

        builder.HasIndex(r => r.DisplayOrder)
            .HasDatabaseName("ix_entity_relationship_metadata_display_order");
    }
}
