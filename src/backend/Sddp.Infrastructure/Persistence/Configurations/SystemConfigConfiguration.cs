using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sddp.Abstractions.ValueObjects;
using Sddp.Domain.Entities;

namespace Sddp.Infrastructure.Persistence.Configurations;

/// <summary>
/// SystemConfig entity EF Core settings
/// </summary>
public class SystemConfigConfiguration : IEntityTypeConfiguration<SystemConfig>
{
    public void Configure(EntityTypeBuilder<SystemConfig> builder)
    {
        builder.ToTable("system_configs");

        // Primary Key
        builder.HasKey(e => e.Id);

        // Id (GlobalUniqueId → Guid)
        builder.Property(e => e.Id)
            .HasColumnName("id")
            .HasConversion(
                id => id.ToGuid(),
                guid => GlobalUniqueId.FromGuid(guid))
            .IsRequired();

        // TenantId (nullable)
        builder.Property(e => e.TenantId)
            .HasColumnName("tenant_id")
            .HasConversion(
                id => id.HasValue ? id.Value.ToGuid() : (Guid?)null,
                guid => guid.HasValue ? GlobalUniqueId.FromGuid(guid.Value) : null);

        // ProjectId (nullable)
        builder.Property(e => e.ProjectId)
            .HasColumnName("project_id")
            .HasConversion(
                id => id.HasValue ? id.Value.ToGuid() : (Guid?)null,
                guid => guid.HasValue ? GlobalUniqueId.FromGuid(guid.Value) : null);

        // ConfigGroup
        builder.Property(e => e.ConfigGroup)
            .HasColumnName("config_group")
            .HasMaxLength(50)
            .IsRequired();

        // ConfigKey
        builder.Property(e => e.ConfigKey)
            .HasColumnName("config_key")
            .HasMaxLength(100)
            .IsRequired();

        // ConfigValue
        builder.Property(e => e.ConfigValue)
            .HasColumnName("config_value")
            .HasColumnType("text");

        // ValueType
        builder.Property(e => e.ValueType)
            .HasColumnName("value_type")
            .HasMaxLength(20)
            .HasDefaultValue("string")
            .IsRequired();

        // DisplayName
        builder.Property(e => e.DisplayName)
            .HasColumnName("display_name")
            .HasMaxLength(200);

        // Description
        builder.Property(e => e.Description)
            .HasColumnName("description")
            .HasColumnType("text");

        // IsSensitive
        builder.Property(e => e.IsSensitive)
            .HasColumnName("is_sensitive")
            .HasDefaultValue(false)
            .IsRequired();

        // IsReadonly
        builder.Property(e => e.IsReadonly)
            .HasColumnName("is_readonly")
            .HasDefaultValue(false)
            .IsRequired();

        // IsSystem
        builder.Property(e => e.IsSystem)
            .HasColumnName("is_system")
            .HasDefaultValue(false)
            .IsRequired();

        // SortOrder
        builder.Property(e => e.SortOrder)
            .HasColumnName("sort_order")
            .HasDefaultValue(0)
            .IsRequired();

        // CreatedAt (Timestamp)
        builder.Property(e => e.CreatedAt)
            .HasColumnName("created_at")
            .HasConversion(
                ts => ts.ToDateTimeOffset(),
                dto => Timestamp.FromDateTimeOffset(dto))
            .IsRequired();

        // UpdatedAt (Timestamp)
        builder.Property(e => e.UpdatedAt)
            .HasColumnName("updated_at")
            .HasConversion(
                ts => ts.ToDateTimeOffset(),
                dto => Timestamp.FromDateTimeOffset(dto))
            .IsRequired();

        // CreatedBy (nullable)
        builder.Property(e => e.CreatedBy)
            .HasColumnName("created_by")
            .HasConversion(
                id => id.HasValue ? id.Value.ToGuid() : (Guid?)null,
                guid => guid.HasValue ? GlobalUniqueId.FromGuid(guid.Value) : null);

        // UpdatedBy (nullable)
        builder.Property(e => e.UpdatedBy)
            .HasColumnName("updated_by")
            .HasConversion(
                id => id.HasValue ? id.Value.ToGuid() : (Guid?)null,
                guid => guid.HasValue ? GlobalUniqueId.FromGuid(guid.Value) : null);

        // IsActive
        builder.Property(e => e.IsActive)
            .HasColumnName("is_active")
            .HasDefaultValue(true)
            .IsRequired();

        // DomainEvents DB
        builder.Ignore(e => e.DomainEvents);

        // Indexes
        builder.HasIndex(e => e.TenantId)
            .HasDatabaseName("idx_system_configs_tenant")
            .HasFilter("tenant_id IS NOT NULL");

        builder.HasIndex(e => new { e.TenantId, e.ProjectId })
            .HasDatabaseName("idx_system_configs_project")
            .HasFilter("project_id IS NOT NULL");

        builder.HasIndex(e => e.ConfigGroup)
            .HasDatabaseName("idx_system_configs_group");

        builder.HasIndex(e => e.ConfigKey)
            .HasDatabaseName("idx_system_configs_key");

        builder.HasIndex(e => new { e.TenantId, e.ProjectId, e.ConfigGroup })
            .HasDatabaseName("idx_system_configs_scope");
    }
}
