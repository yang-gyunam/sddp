using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sddp.Abstractions.Enums;
using Sddp.Abstractions.ValueObjects;
using Sddp.Domain.Entities;

namespace Sddp.Infrastructure.Persistence.Configurations;

public class ProjectConfiguration : IEntityTypeConfiguration<Project>
{
    public void Configure(EntityTypeBuilder<Project> builder)
    {
        builder.ToTable("projects");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Id)
            .HasConversion(
                id => id.ToGuid(),
                guid => GlobalUniqueId.FromGuid(guid))
            .HasColumnName("id");

        builder.Property(p => p.TenantId)
            .HasConversion(
                id => id.ToGuid(),
                guid => GlobalUniqueId.FromGuid(guid))
            .HasColumnName("tenant_id")
            .IsRequired();

        builder.Property(p => p.Code)
            .HasColumnName("code")
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(p => p.Name)
            .HasColumnName("name")
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(p => p.Description)
            .HasColumnName("description")
            .HasMaxLength(2000);

        builder.Property(p => p.Status)
            .HasColumnName("status")
            .HasMaxLength(50)
            .HasDefaultValue(ProjectStatus.Planning)
            .HasConversion(
                s => s.ToString().ToLowerInvariant(),
                s => Enum.Parse<ProjectStatus>(s, true));

        builder.Property(p => p.OwnerId)
            .HasConversion(
                id => id == null ? (Guid?)null : id.Value.ToGuid(),
                guid => guid == null ? null : GlobalUniqueId.FromGuid(guid.Value))
            .HasColumnName("owner_id");

        builder.Property(p => p.RepoUrl)
            .HasColumnName("repo_url")
            .HasMaxLength(2048)
            .IsRequired();

        builder.Property(p => p.RepoBranch)
            .HasColumnName("repo_branch")
            .HasMaxLength(255)
            .HasDefaultValue("main")
            .IsRequired();

        builder.Property(p => p.ArtifactRootPath)
            .HasColumnName("artifact_root_path")
            .HasMaxLength(1024)
            .IsRequired();

        builder.Property(p => p.SyncIntervalMinutes)
            .HasColumnName("sync_interval_minutes")
            .HasDefaultValue(60)
            .IsRequired();

        builder.Property(p => p.LastSyncedAt)
            .HasConversion(
                ts => ts == null ? (DateTimeOffset?)null : ts.Value.ToDateTimeOffset(),
                dto => dto == null ? null : Timestamp.FromDateTimeOffset(dto.Value))
            .HasColumnName("last_synced_at");

        builder.Property(p => p.IsActive)
            .HasColumnName("is_active")
            .HasDefaultValue(true);

        builder.Property(p => p.CreatedAt)
            .HasConversion(
                ts => ts.ToDateTimeOffset(),
                dto => Timestamp.FromDateTimeOffset(dto))
            .HasColumnName("created_at");

        builder.Property(p => p.UpdatedAt)
            .HasConversion(
                ts => ts.ToDateTimeOffset(),
                dto => Timestamp.FromDateTimeOffset(dto))
            .HasColumnName("updated_at");

        //
        builder.HasIndex(p => new { p.TenantId, p.Code }).IsUnique();
        builder.HasIndex(p => new { p.TenantId, p.Name });
        builder.HasIndex(p => p.IsActive);
    }
}
