using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sddp.Abstractions.Constants;
using Sddp.Abstractions.Enums;
using Sddp.Abstractions.ValueObjects;
using Sddp.Domain.Entities;

namespace Sddp.Infrastructure.Persistence.Configurations;

public class RequirementConfiguration : IEntityTypeConfiguration<Requirement>
{
    public void Configure(EntityTypeBuilder<Requirement> builder)
    {
        builder.ToTable("requirements");

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

        builder.Property(r => r.Code)
            .HasColumnName("code")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(r => r.Title)
            .HasColumnName("title")
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(r => r.Description)
            .HasColumnName("description")
            .HasColumnType("text");

        builder.Property(r => r.Level)
            .HasColumnName("level_code_id")
            .HasConversion(
                e => WellKnownCodes.RequirementLevelToId[e],
                id => WellKnownCodes.IdToRequirementLevel[id])
            .IsRequired();

        builder.Property(r => r.Priority)
            .HasColumnName("priority")
            .HasConversion(
                p => p.ToString().ToUpperInvariant(),
                s => Enum.Parse<RequirementPriority>(s, true))
            .HasMaxLength(10)
            .HasDefaultValue(RequirementPriority.Medium)
            .IsRequired();

        builder.Property(r => r.Status)
            .HasColumnName("status_code_id")
            .HasConversion(
                e => WellKnownCodes.RequirementStatusToId[e],
                id => WellKnownCodes.IdToRequirementStatus[id])
            .IsRequired();

        builder.Property(r => r.ParentId)
            .HasConversion(
                id => id.HasValue ? id.Value.ToGuid() : (Guid?)null,
                guid => guid.HasValue ? GlobalUniqueId.FromGuid(guid.Value) : null)
            .HasColumnName("parent_id");

        builder.Property(r => r.OwnerUserId)
            .HasConversion(
                id => id.HasValue ? id.Value.ToGuid() : (Guid?)null,
                guid => guid.HasValue ? GlobalUniqueId.FromGuid(guid.Value) : null)
            .HasColumnName("owner_user_id");

        builder.Property(r => r.ConversationId)
            .HasConversion(
                id => id.HasValue ? id.Value.ToGuid() : (Guid?)null,
                guid => guid.HasValue ? GlobalUniqueId.FromGuid(guid.Value) : null)
            .HasColumnName("conversation_id");

        // VersionedEntityBase
        builder.Property(r => r.Version)
            .HasConversion(
                v => v.ToString(),
                s => SemanticVersion.Parse(s))
            .HasColumnName("version")
            .HasMaxLength(50)
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

        // AuditableEntityBase
        builder.Property(r => r.CreatedBy)
            .HasConversion(
                id => id.ToGuid(),
                guid => GlobalUniqueId.FromGuid(guid))
            .HasColumnName("created_by");

        builder.Property(r => r.UpdatedBy)
            .HasConversion(
                id => id.ToGuid(),
                guid => GlobalUniqueId.FromGuid(guid))
            .HasColumnName("updated_by");

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

        // Self-referencing relationship (Parent-Children)
        builder.HasOne(r => r.Parent)
            .WithMany(r => r.Children)
            .HasForeignKey(r => r.ParentId)
            .OnDelete(DeleteBehavior.Restrict);

        //
        builder.HasIndex(r => new { r.TenantId, r.ProjectId, r.Code })
            .IsUnique()
            .HasDatabaseName("ix_requirements_tenant_project_code");

        builder.HasIndex(r => r.Level)
            .HasDatabaseName("ix_requirements_level");

        builder.HasIndex(r => r.Status)
            .HasDatabaseName("ix_requirements_status");

        builder.HasIndex(r => r.ParentId)
            .HasDatabaseName("ix_requirements_parent_id");

        builder.HasIndex(r => new { r.TenantId, r.ProjectId })
            .HasDatabaseName("ix_requirements_tenant_project");

        builder.HasIndex(r => r.OwnerUserId)
            .HasDatabaseName("ix_requirements_owner");

        builder.HasIndex(r => r.IsActive)
            .HasDatabaseName("ix_requirements_is_active");
    }
}
