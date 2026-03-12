using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sddp.Abstractions.ValueObjects;
using Sddp.Domain.Entities;

namespace Sddp.Infrastructure.Persistence.Configurations;

public class FieldMetadataConfiguration : IEntityTypeConfiguration<FieldMetadata>
{
    public void Configure(EntityTypeBuilder<FieldMetadata> builder)
    {
        builder.ToTable("field_metadata");

        builder.HasKey(f => f.Id);

        builder.Property(f => f.Id)
            .HasConversion(
                id => id.ToGuid(),
                guid => GlobalUniqueId.FromGuid(guid))
            .HasColumnName("id");

        builder.Property(f => f.EntityMetadataId)
            .HasConversion(
                id => id.ToGuid(),
                guid => GlobalUniqueId.FromGuid(guid))
            .HasColumnName("entity_metadata_id")
            .IsRequired();

        builder.Property(f => f.FieldName)
            .HasColumnName("field_name")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(f => f.ColumnName)
            .HasColumnName("column_name")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(f => f.FieldType)
            .HasColumnName("field_type")
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(f => f.IsRequired)
            .HasColumnName("is_required")
            .HasDefaultValue(false);

        builder.Property(f => f.IsUnique)
            .HasColumnName("is_unique")
            .HasDefaultValue(false);

        builder.Property(f => f.MaxLength)
            .HasColumnName("max_length");

        builder.Property(f => f.MinLength)
            .HasColumnName("min_length");

        builder.Property(f => f.ValidationType)
            .HasColumnName("validation_type")
            .HasMaxLength(50);

        builder.Property(f => f.Pattern)
            .HasColumnName("pattern")
            .HasColumnType("text");

        builder.Property(f => f.DefaultValue)
            .HasColumnName("default_value")
            .HasMaxLength(500);

        builder.Property(f => f.Description)
            .HasColumnName("description")
            .HasColumnType("text");

        builder.Property(f => f.DisplayOrder)
            .HasColumnName("display_order")
            .HasDefaultValue(0);

        // EntityBase
        builder.Property(f => f.IsActive)
            .HasColumnName("is_active")
            .HasDefaultValue(true);

        builder.Property(f => f.CreatedAt)
            .HasConversion(
                ts => ts.ToDateTimeOffset(),
                dto => Timestamp.FromDateTimeOffset(dto))
            .HasColumnName("created_at");

        builder.Property(f => f.UpdatedAt)
            .HasConversion(
                ts => ts.ToDateTimeOffset(),
                dto => Timestamp.FromDateTimeOffset(dto))
            .HasColumnName("updated_at");

        // relationship: EntityMetadata
        builder.HasOne(f => f.EntityMetadata)
            .WithMany(e => e.Fields)
            .HasForeignKey(f => f.EntityMetadataId)
            .OnDelete(DeleteBehavior.Cascade);

        //
        builder.HasIndex(f => f.EntityMetadataId)
            .HasDatabaseName("ix_field_metadata_entity_metadata_id");

        builder.HasIndex(f => new { f.EntityMetadataId, f.FieldName })
            .IsUnique()
            .HasDatabaseName("ix_field_metadata_entity_field_name");

        builder.HasIndex(f => f.DisplayOrder)
            .HasDatabaseName("ix_field_metadata_display_order");
    }
}
