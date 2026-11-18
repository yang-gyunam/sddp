using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sddp.Abstractions.Base;
using Sddp.Abstractions.ValueObjects;
using Sddp.Domain.Entities;

namespace Sddp.Infrastructure.Persistence.Configurations;

public sealed class DiagramConfiguration : IEntityTypeConfiguration<Diagram>
{
    public void Configure(EntityTypeBuilder<Diagram> builder)
    {
        DiagramMapping.Common(builder, "diagrams");
        DiagramMapping.Id(builder.Property(d => d.UpdatedBy), "updated_by");
        builder.HasOne<User>().WithMany().HasForeignKey(d => d.UpdatedBy).OnDelete(DeleteBehavior.Restrict);
        builder.Property(d => d.Kind).HasColumnName("kind").HasMaxLength(20).IsRequired();
        builder.Property(d => d.Revision).IsConcurrencyToken();
        builder.ToTable("diagrams", table =>
        {
            table.HasCheckConstraint("ck_diagrams_revision", "revision > 0");
            table.HasCheckConstraint("ck_diagrams_kind", "kind IN ('authored', 'derived')");
            table.HasCheckConstraint("ck_diagrams_model",
                "((model_json->>'diagramId' = id::text AND model_json->>'revision' = revision::text AND model_json->>'schemaVersion' IN ('1', '2')) IS TRUE)");
        });
        builder.HasAlternateKey(d => new { d.TenantId, d.ProjectId, d.Id }).HasName("uq_diagrams_scope");
        builder.HasIndex(d => new { d.TenantId, d.ProjectId, d.UpdatedAt }).HasDatabaseName("ix_diagrams_project");
        builder.HasOne<Project>().WithMany().HasForeignKey(d => new { d.TenantId, d.ProjectId })
            .HasPrincipalKey(p => new { p.TenantId, p.Id }).OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_diagrams_project");
    }
}

public sealed class DiagramVersionConfiguration : IEntityTypeConfiguration<DiagramVersion>
{
    public void Configure(EntityTypeBuilder<DiagramVersion> builder)
    {
        DiagramMapping.Common(builder, "diagram_versions");
        DiagramMapping.Id(builder.Property(v => v.DiagramId), "diagram_id");
        builder.ToTable("diagram_versions", table =>
        {
            table.HasCheckConstraint("ck_diagram_versions_revision", "revision > 0");
            table.HasCheckConstraint("ck_diagram_versions_model",
                "((model_json->>'diagramId' = diagram_id::text AND model_json->>'revision' = revision::text AND model_json->>'schemaVersion' IN ('1', '2')) IS TRUE)");
        });
        builder.Property(v => v.ChangeSummary).HasColumnName("change_summary").HasMaxLength(500);
        builder.HasIndex(v => new { v.DiagramId, v.Revision }).IsUnique().HasDatabaseName("uq_diagram_versions_revision");
        builder.HasIndex(v => new { v.TenantId, v.ProjectId, v.DiagramId }).HasDatabaseName("ix_diagram_versions_scope");
        builder.HasOne<Diagram>().WithMany().HasForeignKey(v => new { v.TenantId, v.ProjectId, v.DiagramId })
            .HasPrincipalKey(d => new { d.TenantId, d.ProjectId, d.Id }).OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_diagram_versions_diagram");
    }
}

internal static class DiagramMapping
{
    internal static void Common<T>(EntityTypeBuilder<T> builder, string table) where T : EntityBase
    {
        builder.ToTable(table);
        builder.HasKey(e => e.Id);
        Id(builder.Property(e => e.Id), "id");
        Id(builder.Property<GlobalUniqueId>("TenantId"), "tenant_id");
        Id(builder.Property<GlobalUniqueId>("ProjectId"), "project_id");
        Id(builder.Property<GlobalUniqueId>("CreatedBy"), "created_by");
        builder.Property<string>("Title").HasColumnName("title").HasMaxLength(200).IsRequired();
        builder.Property<int>("Revision").HasColumnName("revision").IsRequired();
        builder.Property<string>("ModelJson").HasColumnName("model_json").HasColumnType("jsonb").IsRequired();
        builder.Property(e => e.CreatedAt).HasColumnName("created_at")
            .HasConversion(v => v.ToDateTimeOffset(), v => Timestamp.FromDateTimeOffset(v));
        builder.Property(e => e.UpdatedAt).HasColumnName("updated_at")
            .HasConversion(v => v.ToDateTimeOffset(), v => Timestamp.FromDateTimeOffset(v));
        builder.Property(e => e.IsActive).HasColumnName("is_active").HasDefaultValue(true);
        builder.HasOne<User>().WithMany().HasForeignKey("CreatedBy").OnDelete(DeleteBehavior.Restrict);
    }

    internal static void Id(PropertyBuilder<GlobalUniqueId> property, string column) =>
        property.HasColumnName(column).HasConversion(v => v.ToGuid(), v => GlobalUniqueId.FromGuid(v)).IsRequired();
}
