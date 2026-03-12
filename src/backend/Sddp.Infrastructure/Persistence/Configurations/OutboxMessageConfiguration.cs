using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sddp.Abstractions.ValueObjects;
using Sddp.Domain.Entities;

namespace Sddp.Infrastructure.Persistence.Configurations;

/// <summary>
/// OutboxMessage entity EF Core settings
/// </summary>
public class OutboxMessageConfiguration : IEntityTypeConfiguration<OutboxMessage>
{
    public void Configure(EntityTypeBuilder<OutboxMessage> builder)
    {
        builder.ToTable("outbox_messages");

        // Primary Key
        builder.HasKey(e => e.Id);

        // Id (GlobalUniqueId → Guid)
        builder.Property(e => e.Id)
            .HasColumnName("id")
            .HasConversion(
                id => id.ToGuid(),
                guid => GlobalUniqueId.FromGuid(guid))
            .IsRequired();

        // Event Type
        builder.Property(e => e.EventType)
            .HasColumnName("event_type")
            .HasMaxLength(256)
            .IsRequired();

        // Aggregate Type
        builder.Property(e => e.AggregateType)
            .HasColumnName("aggregate_type")
            .HasMaxLength(256)
            .IsRequired();

        // Aggregate Id
        builder.Property(e => e.AggregateId)
            .HasColumnName("aggregate_id")
            .HasConversion(
                id => id.ToGuid(),
                guid => GlobalUniqueId.FromGuid(guid))
            .IsRequired();

        // Payload (JSON)
        builder.Property(e => e.Payload)
            .HasColumnName("payload")
            .HasColumnType("jsonb")
            .IsRequired();

        // Processed At
        builder.Property(e => e.ProcessedAt)
            .HasColumnName("processed_at")
            .HasConversion(
                ts => ts.HasValue ? ts.Value.ToDateTimeOffset() : (DateTimeOffset?)null,
                dto => dto.HasValue ? Timestamp.FromDateTimeOffset(dto.Value) : null);

        // Retry Count
        builder.Property(e => e.RetryCount)
            .HasColumnName("retry_count")
            .IsRequired()
            .HasDefaultValue(0);

        // Last Error
        builder.Property(e => e.LastError)
            .HasColumnName("last_error")
            .HasMaxLength(4000);

        // Created At (Timestamp)
        builder.Property(e => e.CreatedAt)
            .HasColumnName("created_at")
            .HasConversion(
                ts => ts.ToDateTimeOffset(),
                dto => Timestamp.FromDateTimeOffset(dto))
            .IsRequired();

        // Updated At (Timestamp)
        builder.Property(e => e.UpdatedAt)
            .HasColumnName("updated_at")
            .HasConversion(
                ts => ts.ToDateTimeOffset(),
                dto => Timestamp.FromDateTimeOffset(dto))
            .IsRequired();

        // IsActive
        builder.Property(e => e.IsActive)
            .HasColumnName("is_active")
            .IsRequired()
            .HasDefaultValue(true);

        // DomainEvents DB
        builder.Ignore(e => e.DomainEvents);

        // : message get
        builder.HasIndex(e => new { e.ProcessedAt, e.CreatedAt })
            .HasDatabaseName("ix_outbox_messages_pending");

        // : get
        builder.HasIndex(e => e.EventType)
            .HasDatabaseName("ix_outbox_messages_event_type");

        // : aggregated get
        builder.HasIndex(e => new { e.AggregateType, e.AggregateId })
            .HasDatabaseName("ix_outbox_messages_aggregate");
    }
}
