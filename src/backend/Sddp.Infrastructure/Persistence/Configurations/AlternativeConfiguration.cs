using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sddp.Abstractions.ValueObjects;
using Sddp.Domain.Entities;

namespace Sddp.Infrastructure.Persistence.Configurations;

public class AlternativeConfiguration : IEntityTypeConfiguration<Alternative>
{
    public void Configure(EntityTypeBuilder<Alternative> builder)
    {
        builder.ToTable("alternatives");

        builder.HasKey(a => a.Id);

        builder.Property(a => a.Id)
            .HasConversion(
                id => id.ToGuid(),
                guid => GlobalUniqueId.FromGuid(guid))
            .HasColumnName("id");

        builder.Property(a => a.TenantId)
            .HasConversion(
                id => id.ToGuid(),
                guid => GlobalUniqueId.FromGuid(guid))
            .HasColumnName("tenant_id")
            .IsRequired();

        builder.Property(a => a.ProjectId)
            .HasConversion(
                id => id.ToGuid(),
                guid => GlobalUniqueId.FromGuid(guid))
            .HasColumnName("project_id")
            .IsRequired();

        builder.Property(a => a.SpecId)
            .HasConversion(
                id => id.ToGuid(),
                guid => GlobalUniqueId.FromGuid(guid))
            .HasColumnName("spec_id")
            .IsRequired();

        builder.Property(a => a.Description)
            .HasColumnName("description")
            .HasColumnType("text")
            .IsRequired();

        builder.Property(a => a.RejectedReason)
            .HasColumnName("rejected_reason")
            .HasColumnType("text");

        builder.Property(a => a.ProposedBy)
            .HasConversion(
                id => id.ToGuid(),
                guid => GlobalUniqueId.FromGuid(guid))
            .HasColumnName("proposed_by")
            .IsRequired();

        builder.Property(a => a.ProposedAt)
            .HasConversion(
                ts => ts.ToDateTimeOffset(),
                dto => Timestamp.FromDateTimeOffset(dto))
            .HasColumnName("proposed_at");

        builder.Property(a => a.Order)
            .HasColumnName("order")
            .HasDefaultValue(0);

        // AuditableEntityBase
        builder.Property(a => a.CreatedBy)
            .HasConversion(
                id => id.ToGuid(),
                guid => GlobalUniqueId.FromGuid(guid))
            .HasColumnName("created_by");

        builder.Property(a => a.UpdatedBy)
            .HasConversion(
                id => id.ToGuid(),
                guid => GlobalUniqueId.FromGuid(guid))
            .HasColumnName("updated_by");

        // EntityBase
        builder.Property(a => a.IsActive)
            .HasColumnName("is_active")
            .HasDefaultValue(true);

        builder.Property(a => a.CreatedAt)
            .HasConversion(
                ts => ts.ToDateTimeOffset(),
                dto => Timestamp.FromDateTimeOffset(dto))
            .HasColumnName("created_at");

        builder.Property(a => a.UpdatedAt)
            .HasConversion(
                ts => ts.ToDateTimeOffset(),
                dto => Timestamp.FromDateTimeOffset(dto))
            .HasColumnName("updated_at");

        // IsRejected
        builder.Ignore(a => a.IsRejected);

        // relationship: Spec
        builder.HasOne(a => a.Spec)
            .WithMany(s => s.Alternatives)
            .HasForeignKey(a => a.SpecId)
            .OnDelete(DeleteBehavior.Cascade);

        // relationship: Proposer (User)
        builder.HasOne(a => a.Proposer)
            .WithMany()
            .HasForeignKey(a => a.ProposedBy)
            .OnDelete(DeleteBehavior.Restrict);

        //
        builder.HasIndex(a => a.SpecId)
            .HasDatabaseName("ix_alternatives_spec_id");

        builder.HasIndex(a => a.ProposedBy)
            .HasDatabaseName("ix_alternatives_proposed_by");

        builder.HasIndex(a => new { a.TenantId, a.ProjectId })
            .HasDatabaseName("ix_alternatives_tenant_project");

        builder.HasIndex(a => new { a.SpecId, a.Order })
            .HasDatabaseName("ix_alternatives_spec_order");

        builder.HasIndex(a => a.IsActive)
            .HasDatabaseName("ix_alternatives_is_active");
    }
}
