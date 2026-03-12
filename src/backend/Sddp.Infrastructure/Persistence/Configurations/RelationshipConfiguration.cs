using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sddp.Abstractions.Constants;
using Sddp.Abstractions.ValueObjects;
using Sddp.Domain.Entities;

namespace Sddp.Infrastructure.Persistence.Configurations;

public class RelationshipConfiguration : IEntityTypeConfiguration<Relationship>
{
    public void Configure(EntityTypeBuilder<Relationship> builder)
    {
        builder.ToTable("relationships");

        builder.HasKey(r => r.Id);

        builder.Property(r => r.Id)
            .HasConversion(
                id => id.ToGuid(),
                guid => GlobalUniqueId.FromGuid(guid))
            .HasColumnName("id");

        builder.Property(r => r.TenantId)
            .HasConversion(
                id => id.ToGuid(),
                guid => GlobalUniqueId.FromGuid(guid))
            .HasColumnName("tenant_id")
            .IsRequired();

        builder.Property(r => r.ProjectId)
            .HasConversion(
                id => id.ToGuid(),
                guid => GlobalUniqueId.FromGuid(guid))
            .HasColumnName("project_id")
            .IsRequired();

        builder.Property(r => r.FromEntityType)
            .HasColumnName("from_entity_type")
            .HasConversion<int>()
            .IsRequired();

        builder.Property(r => r.FromEntityId)
            .HasConversion(
                id => id.ToGuid(),
                guid => GlobalUniqueId.FromGuid(guid))
            .HasColumnName("from_entity_id")
            .IsRequired();

        builder.Property(r => r.ToEntityType)
            .HasColumnName("to_entity_type")
            .HasConversion<int>()
            .IsRequired();

        builder.Property(r => r.ToEntityId)
            .HasConversion(
                id => id.ToGuid(),
                guid => GlobalUniqueId.FromGuid(guid))
            .HasColumnName("to_entity_id")
            .IsRequired();

        builder.Property(r => r.Type)
            .HasColumnName("type_code_id")
            .HasConversion(
                e => WellKnownCodes.RelationTypeToId[e],
                id => WellKnownCodes.IdToRelationType[id])
            .IsRequired();

        builder.Property(r => r.ValidFrom)
            .HasConversion(
                ts => ts.ToDateTimeOffset(),
                dto => Timestamp.FromDateTimeOffset(dto))
            .HasColumnName("valid_from");

        builder.Property(r => r.ValidTo)
            .HasConversion(
                ts => ts.HasValue ? ts.Value.ToDateTimeOffset() : (DateTimeOffset?)null,
                dto => dto.HasValue ? Timestamp.FromDateTimeOffset(dto.Value) : null)
            .HasColumnName("valid_to");

        builder.Property(r => r.Reason)
            .HasColumnName("reason")
            .HasColumnType("text");

        builder.Property(r => r.SourceSpecId)
            .HasConversion(
                id => id.HasValue ? id.Value.ToGuid() : (Guid?)null,
                guid => guid.HasValue ? GlobalUniqueId.FromGuid(guid.Value) : null)
            .HasColumnName("source_spec_id");

        builder.Property(r => r.SourceDecisionId)
            .HasConversion(
                id => id.HasValue ? id.Value.ToGuid() : (Guid?)null,
                guid => guid.HasValue ? GlobalUniqueId.FromGuid(guid.Value) : null)
            .HasColumnName("source_decision_id");

        builder.Property(r => r.CreatedBy)
            .HasConversion(
                id => id.ToGuid(),
                guid => GlobalUniqueId.FromGuid(guid))
            .HasColumnName("created_by")
            .IsRequired();

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

        // Computed property
        builder.Ignore(r => r.IsCurrent);

        // relationship: SourceSpec ()
        builder.HasOne(r => r.SourceSpec)
            .WithMany()
            .HasForeignKey(r => r.SourceSpecId)
            .OnDelete(DeleteBehavior.SetNull);

        // :
        builder.HasIndex(r => new { r.TenantId, r.ProjectId })
            .HasDatabaseName("ix_relationships_tenant_project");

        // : entity get
        builder.HasIndex(r => new { r.FromEntityType, r.FromEntityId })
            .HasDatabaseName("ix_relationships_from_entity");

        // : entity get
        builder.HasIndex(r => new { r.ToEntityType, r.ToEntityId })
            .HasDatabaseName("ix_relationships_to_entity");

        // : relationship type get
        builder.HasIndex(r => r.Type)
            .HasDatabaseName("ix_relationships_type");

        // : relationship get
        builder.HasIndex(r => r.ValidTo)
            .HasDatabaseName("ix_relationships_valid_to");

        // : relationship (relationship)
        builder.HasIndex(r => new {
            r.TenantId,
            r.ProjectId,
            r.FromEntityType,
            r.FromEntityId,
            r.ToEntityType,
            r.ToEntityId,
            r.Type
        })
        .HasFilter("valid_to IS NULL")
        .IsUnique()
        .HasDatabaseName("ix_relationships_unique_active");

        builder.HasIndex(r => r.IsActive)
            .HasDatabaseName("ix_relationships_is_active");
    }
}
