using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sddp.Abstractions.ValueObjects;
using Sddp.Domain.Entities;

namespace Sddp.Infrastructure.Persistence.Configurations;

public class LegacyIdMappingConfiguration : IEntityTypeConfiguration<LegacyIdMapping>
{
    public void Configure(EntityTypeBuilder<LegacyIdMapping> builder)
    {
        builder.ToTable("legacy_id_mappings");

        builder.HasKey(m => m.Id);

        builder.Property(m => m.Id)
            .HasConversion(
                id => id.ToGuid(),
                guid => GlobalUniqueId.FromGuid(guid))
            .HasColumnName("id");

        builder.Property(m => m.EntityType)
            .HasColumnName("entity_type")
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(m => m.EntityId)
            .HasConversion(
                id => id.ToGuid(),
                guid => GlobalUniqueId.FromGuid(guid))
            .HasColumnName("entity_id")
            .IsRequired();

        builder.Property(m => m.LegacySystem)
            .HasColumnName("legacy_system")
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(m => m.LegacyId)
            .HasColumnName("legacy_id")
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(m => m.Metadata)
            .HasColumnName("metadata")
            .HasColumnType("jsonb");

        builder.Property(m => m.IsActive)
            .HasColumnName("is_active")
            .HasDefaultValue(true);

        builder.Property(m => m.CreatedAt)
            .HasConversion(
                ts => ts.ToDateTimeOffset(),
                dto => Timestamp.FromDateTimeOffset(dto))
            .HasColumnName("created_at");

        builder.Property(m => m.UpdatedAt)
            .HasConversion(
                ts => ts.ToDateTimeOffset(),
                dto => Timestamp.FromDateTimeOffset(dto))
            .HasColumnName("updated_at");

        // (system + ID get)
        builder.HasIndex(m => new { m.LegacySystem, m.LegacyId }).IsUnique();
        builder.HasIndex(m => new { m.EntityType, m.EntityId });
    }
}
