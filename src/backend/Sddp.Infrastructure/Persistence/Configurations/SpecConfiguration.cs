using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sddp.Abstractions.Constants;
using Sddp.Abstractions.ValueObjects;
using Sddp.Domain.Entities;

namespace Sddp.Infrastructure.Persistence.Configurations;

public class SpecConfiguration : IEntityTypeConfiguration<Spec>
{
    public void Configure(EntityTypeBuilder<Spec> builder)
    {
        builder.ToTable("specs");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.Id)
            .HasConversion(
                id => id.ToGuid(),
                guid => GlobalUniqueId.FromGuid(guid))
            .HasColumnName("id");

        builder.Property(s => s.TenantId)
            .HasConversion(
                id => id.ToGuid(),
                guid => GlobalUniqueId.FromGuid(guid))
            .HasColumnName("tenant_id")
            .IsRequired();

        builder.Property(s => s.ProjectId)
            .HasConversion(
                id => id.ToGuid(),
                guid => GlobalUniqueId.FromGuid(guid))
            .HasColumnName("project_id")
            .IsRequired();

        builder.Property(s => s.Code)
            .HasColumnName("code")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(s => s.Title)
            .HasColumnName("title")
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(s => s.Description)
            .HasColumnName("description")
            .HasColumnType("text");

        builder.Property(s => s.Decision)
            .HasColumnName("decision")
            .HasColumnType("text");

        builder.Property(s => s.Context)
            .HasColumnName("context")
            .HasColumnType("text");

        builder.Property(s => s.Scope)
            .HasColumnName("scope")
            .HasColumnType("text");

        builder.Property(s => s.OutOfScope)
            .HasColumnName("out_of_scope")
            .HasColumnType("text");

        builder.Property(s => s.Definitions)
            .HasColumnName("definitions")
            .HasColumnType("text");

        builder.Property(s => s.AcceptanceCriteria)
            .HasColumnName("acceptance_criteria")
            .HasColumnType("text");

        builder.Property(s => s.Owners)
            .HasConversion(
                owners => owners.ToCsv(),
                value => SpecOwners.FromCsv(value))
            .HasColumnName("owners")
            .HasColumnType("text")
            .IsRequired()
            .HasDefaultValueSql("''");

        builder.Property(s => s.ReviewTrigger)
            .HasColumnName("review_trigger")
            .HasColumnType("text");

        builder.Property(s => s.Status)
            .HasColumnName("status_code_id")
            .HasConversion(
                e => WellKnownCodes.SpecStatusToId[e],
                id => WellKnownCodes.IdToSpecStatus[id])
            .IsRequired();

        builder.Property(s => s.RequirementId)
            .HasConversion(
                id => id.HasValue ? id.Value.ToGuid() : (Guid?)null,
                guid => guid.HasValue ? GlobalUniqueId.FromGuid(guid.Value) : null)
            .HasColumnName("requirement_id");

        builder.Property(s => s.BornFromConversationId)
            .HasConversion(
                id => id.HasValue ? id.Value.ToGuid() : (Guid?)null,
                guid => guid.HasValue ? GlobalUniqueId.FromGuid(guid.Value) : null)
            .HasColumnName("born_from_conversation_id");

        builder.Property(s => s.SupersedesSpecId)
            .HasConversion(
                id => id.HasValue ? id.Value.ToGuid() : (Guid?)null,
                guid => guid.HasValue ? GlobalUniqueId.FromGuid(guid.Value) : null)
            .HasColumnName("supersedes_spec_id");

        builder.Property(s => s.LockedAt)
            .HasConversion(
                ts => ts.HasValue ? ts.Value.ToDateTimeOffset() : (DateTimeOffset?)null,
                dto => dto.HasValue ? Timestamp.FromDateTimeOffset(dto.Value) : null)
            .HasColumnName("locked_at");

        // VersionedEntityBase
        builder.Property(s => s.Version)
            .HasConversion(
                v => v.ToString(),
                str => SemanticVersion.Parse(str))
            .HasColumnName("version")
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(s => s.ValidFrom)
            .HasConversion(
                ts => ts.ToDateTimeOffset(),
                dto => Timestamp.FromDateTimeOffset(dto))
            .HasColumnName("valid_from");

        builder.Property(s => s.ValidTo)
            .HasConversion(
                ts => ts.HasValue ? ts.Value.ToDateTimeOffset() : (DateTimeOffset?)null,
                dto => dto.HasValue ? Timestamp.FromDateTimeOffset(dto.Value) : null)
            .HasColumnName("valid_to");

        // AuditableEntityBase
        builder.Property(s => s.CreatedBy)
            .HasConversion(
                id => id.ToGuid(),
                guid => GlobalUniqueId.FromGuid(guid))
            .HasColumnName("created_by");

        builder.Property(s => s.UpdatedBy)
            .HasConversion(
                id => id.ToGuid(),
                guid => GlobalUniqueId.FromGuid(guid))
            .HasColumnName("updated_by");

        // EntityBase
        builder.Property(s => s.IsActive)
            .HasColumnName("is_active")
            .HasDefaultValue(true);

        builder.Property(s => s.CreatedAt)
            .HasConversion(
                ts => ts.ToDateTimeOffset(),
                dto => Timestamp.FromDateTimeOffset(dto))
            .HasColumnName("created_at");

        builder.Property(s => s.UpdatedAt)
            .HasConversion(
                ts => ts.ToDateTimeOffset(),
                dto => Timestamp.FromDateTimeOffset(dto))
            .HasColumnName("updated_at");

        // relationship: Requirement
        builder.HasOne(s => s.Requirement)
            .WithMany()
            .HasForeignKey(s => s.RequirementId)
            .OnDelete(DeleteBehavior.SetNull);

        // relationship: SupersedesSpec (self-referencing)
        builder.HasOne(s => s.SupersedesSpec)
            .WithMany()
            .HasForeignKey(s => s.SupersedesSpecId)
            .OnDelete(DeleteBehavior.Restrict);

        //
        builder.HasIndex(s => new { s.TenantId, s.ProjectId, s.Code })
            .IsUnique()
            .HasFilter("valid_to IS NULL")
            .HasDatabaseName("ix_specs_tenant_project_code");

        builder.HasIndex(s => s.Status)
            .HasDatabaseName("ix_specs_status");

        builder.HasIndex(s => s.RequirementId)
            .HasDatabaseName("ix_specs_requirement_id");

        builder.HasIndex(s => new { s.TenantId, s.ProjectId })
            .HasDatabaseName("ix_specs_tenant_project");

        builder.HasIndex(s => s.IsActive)
            .HasDatabaseName("ix_specs_is_active");

        builder.HasIndex(s => s.BornFromConversationId)
            .HasDatabaseName("ix_specs_born_from_conversation_id");
    }
}
