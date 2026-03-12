using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sddp.Abstractions.Constants;
using Sddp.Abstractions.ValueObjects;
using Sddp.Domain.Entities;

namespace Sddp.Infrastructure.Persistence.Configurations;

public class SignOffConfiguration : IEntityTypeConfiguration<SignOff>
{
    public void Configure(EntityTypeBuilder<SignOff> builder)
    {
        builder.ToTable("sign_offs");

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

        builder.Property(s => s.SpecId)
            .HasConversion(
                id => id.ToGuid(),
                guid => GlobalUniqueId.FromGuid(guid))
            .HasColumnName("spec_id")
            .IsRequired();

        builder.Property(s => s.StakeholderId)
            .HasConversion(
                id => id.ToGuid(),
                guid => GlobalUniqueId.FromGuid(guid))
            .HasColumnName("stakeholder_id")
            .IsRequired();

        builder.Property(s => s.Role)
            .HasColumnName("role")
            .HasConversion<int>()
            .IsRequired();

        builder.Property(s => s.Decision)
            .HasColumnName("decision_code_id")
            .HasConversion(
                e => WellKnownCodes.SignOffDecisionToId[e],
                id => WellKnownCodes.IdToSignOffDecision[id])
            .IsRequired();

        builder.Property(s => s.Conditions)
            .HasColumnName("conditions")
            .HasColumnType("text");

        builder.Property(s => s.Comments)
            .HasColumnName("comments")
            .HasColumnType("text");

        builder.Property(s => s.RequestedAt)
            .HasConversion(
                ts => ts.ToDateTimeOffset(),
                dto => Timestamp.FromDateTimeOffset(dto))
            .HasColumnName("requested_at")
            .IsRequired();

        builder.Property(s => s.SignedAt)
            .HasConversion(
                ts => ts.HasValue ? ts.Value.ToDateTimeOffset() : (DateTimeOffset?)null,
                dto => dto.HasValue ? Timestamp.FromDateTimeOffset(dto.Value) : null)
            .HasColumnName("signed_at");

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

        // relationship: Spec
        builder.HasOne(s => s.Spec)
            .WithMany(spec => spec.SignOffs)
            .HasForeignKey(s => s.SpecId)
            .OnDelete(DeleteBehavior.Cascade);

        // relationship: Stakeholder (User)
        builder.HasOne(s => s.Stakeholder)
            .WithMany()
            .HasForeignKey(s => s.StakeholderId)
            .OnDelete(DeleteBehavior.Restrict);

        // : Spec user
        builder.HasIndex(s => new { s.SpecId, s.StakeholderId })
            .IsUnique()
            .HasDatabaseName("ix_sign_offs_spec_stakeholder");

        builder.HasIndex(s => s.SpecId)
            .HasDatabaseName("ix_sign_offs_spec_id");

        builder.HasIndex(s => s.StakeholderId)
            .HasDatabaseName("ix_sign_offs_stakeholder_id");

        builder.HasIndex(s => new { s.TenantId, s.ProjectId })
            .HasDatabaseName("ix_sign_offs_tenant_project");

        builder.HasIndex(s => s.Decision)
            .HasDatabaseName("ix_sign_offs_decision");

        builder.HasIndex(s => s.IsActive)
            .HasDatabaseName("ix_sign_offs_is_active");
    }
}
