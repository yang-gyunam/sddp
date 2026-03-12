using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sddp.Abstractions.ValueObjects;
using Sddp.Domain.Entities;

namespace Sddp.Infrastructure.Persistence.Configurations;

/// <summary>
/// AuditLog entity EF Core settings
/// </summary>
public class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.ToTable("audit_logs");

        // Primary Key
        builder.HasKey(e => e.Id);

        // Id (GlobalUniqueId → Guid)
        builder.Property(e => e.Id)
            .HasColumnName("id")
            .HasConversion(
                id => id.ToGuid(),
                guid => GlobalUniqueId.FromGuid(guid))
            .IsRequired();

        // Actor Id (nullable)
        builder.Property(e => e.ActorId)
            .HasColumnName("actor_id")
            .HasConversion(
                id => id.HasValue ? id.Value.ToGuid() : (Guid?)null,
                guid => guid.HasValue ? GlobalUniqueId.FromGuid(guid.Value) : null);

        // Action
        builder.Property(e => e.Action)
            .HasColumnName("action")
            .HasMaxLength(64)
            .IsRequired();

        // Resource Type
        builder.Property(e => e.ResourceType)
            .HasColumnName("resource_type")
            .HasMaxLength(128)
            .IsRequired();

        // Resource Id
        builder.Property(e => e.ResourceId)
            .HasColumnName("resource_id")
            .HasConversion(
                id => id.ToGuid(),
                guid => GlobalUniqueId.FromGuid(guid))
            .IsRequired();

        // Payload (JSON)
        builder.Property(e => e.Payload)
            .HasColumnName("payload")
            .HasColumnType("jsonb");

        // IP Address
        builder.Property(e => e.IpAddress)
            .HasColumnName("ip_address")
            .HasMaxLength(45); // IPv6 max length

        // User Agent
        builder.Property(e => e.UserAgent)
            .HasColumnName("user_agent")
            .HasMaxLength(512);

        // Correlation Id
        builder.Property(e => e.CorrelationId)
            .HasColumnName("correlation_id")
            .HasMaxLength(64);

        // Tenant Id (nullable)
        builder.Property(e => e.TenantId)
            .HasColumnName("tenant_id")
            .HasConversion(
                id => id.HasValue ? id.Value.ToGuid() : (Guid?)null,
                guid => guid.HasValue ? GlobalUniqueId.FromGuid(guid.Value) : null);

        // Project Id (nullable)
        builder.Property(e => e.ProjectId)
            .HasColumnName("project_id")
            .HasConversion(
                id => id.HasValue ? id.Value.ToGuid() : (Guid?)null,
                guid => guid.HasValue ? GlobalUniqueId.FromGuid(guid.Value) : null);

        // Created At (Timestamp)
        builder.Property(e => e.CreatedAt)
            .HasColumnName("created_at")
            .HasConversion(
                ts => ts.ToDateTimeOffset(),
                dto => Timestamp.FromDateTimeOffset(dto))
            .IsRequired();

        // audit_logs updated_at
        builder.Ignore(e => e.UpdatedAt);

        // IsActive
        builder.Property(e => e.IsActive)
            .HasColumnName("is_active")
            .IsRequired()
            .HasDefaultValue(true);

        // DomainEvents DB
        builder.Ignore(e => e.DomainEvents);

        // : get
        builder.HasIndex(e => e.CreatedAt)
            .HasDatabaseName("ix_audit_logs_created_at");

        // : Actor get
        builder.HasIndex(e => e.ActorId)
            .HasDatabaseName("ix_audit_logs_actor_id");

        // : Resource get
        builder.HasIndex(e => new { e.ResourceType, e.ResourceId })
            .HasDatabaseName("ix_audit_logs_resource");

        // : Action get
        builder.HasIndex(e => e.Action)
            .HasDatabaseName("ix_audit_logs_action");

        // : Correlation ID get
        builder.HasIndex(e => e.CorrelationId)
            .HasDatabaseName("ix_audit_logs_correlation_id");
    }
}
