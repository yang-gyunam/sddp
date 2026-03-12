using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Sddp.Abstractions.Constants;
using Sddp.Abstractions.ValueObjects;
using Sddp.Domain.Entities;

namespace Sddp.Infrastructure.Persistence.Configurations;

public class MessageConfiguration : IEntityTypeConfiguration<Message>
{
    // Value converter that handles NULL database values for string[] References
    // convertsNulls is internal but required for null DB value handling; no public alternative exists
#pragma warning disable EF1001
    private static readonly ValueConverter<string[], string> ReferencesConverter = new(
        v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
        v => string.IsNullOrEmpty(v)
            ? Array.Empty<string>()
            : JsonSerializer.Deserialize<string[]>(v, (JsonSerializerOptions?)null) ?? Array.Empty<string>(),
        convertsNulls: true);
#pragma warning restore EF1001

    public void Configure(EntityTypeBuilder<Message> builder)
    {
        builder.ToTable("messages");

        builder.HasKey(m => m.Id);

        builder.Property(m => m.Id)
            .HasConversion(
                id => id.ToGuid(),
                guid => GlobalUniqueId.FromGuid(guid))
            .HasColumnName("id");

        builder.Property(m => m.TenantId)
            .HasConversion(
                id => id.ToGuid(),
                guid => GlobalUniqueId.FromGuid(guid))
            .HasColumnName("tenant_id")
            .IsRequired();

        builder.Property(m => m.ProjectId)
            .HasConversion(
                id => id.HasValue ? id.Value.ToGuid() : (Guid?)null,
                guid => guid.HasValue ? GlobalUniqueId.FromGuid(guid.Value) : null)
            .HasColumnName("project_id");

        // Conversation ID - conversations FK (Channel/DirectMessage message)
        builder.Property(m => m.ConversationId)
            .HasConversion(
                id => id.HasValue ? id.Value.ToGuid() : (Guid?)null,
                guid => guid.HasValue ? GlobalUniqueId.FromGuid(guid.Value) : null)
            .HasColumnName("conversation_id");

        // Topic ID - topics FK (Forum Topic message)
        builder.Property(m => m.TopicId)
            .HasConversion(
                id => id.HasValue ? id.Value.ToGuid() : (Guid?)null,
                guid => guid.HasValue ? GlobalUniqueId.FromGuid(guid.Value) : null)
            .HasColumnName("topic_id");

        builder.Property(m => m.SenderId)
            .HasConversion(
                id => id.ToGuid(),
                guid => GlobalUniqueId.FromGuid(guid))
            .HasColumnName("sender_id")
            .IsRequired();

        builder.Property(m => m.Type)
            .HasColumnName("type_code_id")
            .HasConversion(
                e => WellKnownCodes.MessageTypeToId[e],
                id => WellKnownCodes.IdToMessageType[id])
            .IsRequired();

        builder.Property(m => m.Content)
            .HasColumnName("content")
            .HasColumnType("text")
            .IsRequired();

        // References JSONB (NULL)
        builder.Property(m => m.References)
            .HasColumnName("references")
            .HasColumnType("jsonb")
            .HasConversion(ReferencesConverter);

        builder.Property(m => m.ReplyToId)
            .HasConversion(
                id => id.HasValue ? id.Value.ToGuid() : (Guid?)null,
                guid => guid.HasValue ? GlobalUniqueId.FromGuid(guid.Value) : null)
            .HasColumnName("reply_to_id");

        builder.Property(m => m.IsEdited)
            .HasColumnName("is_edited")
            .HasDefaultValue(false);

        builder.Property(m => m.EditedAt)
            .HasConversion(
                ts => ts.HasValue ? ts.Value.ToDateTimeOffset() : (DateTimeOffset?)null,
                dto => dto.HasValue ? Timestamp.FromDateTimeOffset(dto.Value) : null)
            .HasColumnName("edited_at");

        builder.Property(m => m.IsPinned)
            .HasColumnName("is_pinned")
            .HasDefaultValue(false);

        builder.Property(m => m.IsActive)
            .HasColumnName("is_active")
            .HasDefaultValue(true);

        builder.Property(m => m.CreatedAt)
            .HasConversion(
                ts => ts.ToDateTimeOffset(),
                dto => Timestamp.FromDateTimeOffset(dto))
            .HasColumnName("created_at");

        builder.Property(m => m.UpdatedAt)
            .HasConversion(
                ts => ts.ToDateTimeOffset(),
                dto => Timestamp.FromDateTimeOffset(dto))
            .HasColumnName("updated_at");

        // Conversation relationship - ConversationConfiguration settings

        builder.HasOne(m => m.Sender)
            .WithMany()
            .HasForeignKey(m => m.SenderId)
            .OnDelete(DeleteBehavior.Restrict);

        // Self-reference (Reply)
        builder.HasOne(m => m.ReplyTo)
            .WithMany()
            .HasForeignKey(m => m.ReplyToId)
            .OnDelete(DeleteBehavior.SetNull);

        //
        builder.HasIndex(m => m.ConversationId)
            .HasDatabaseName("idx_messages_conversation_id")
            .HasFilter("conversation_id IS NOT NULL");
        builder.HasIndex(m => m.TopicId)
            .HasDatabaseName("idx_messages_topic_id")
            .HasFilter("topic_id IS NOT NULL");
        builder.HasIndex(m => m.SenderId)
            .HasDatabaseName("idx_messages_sender_id");
        builder.HasIndex(m => m.Type)
            .HasDatabaseName("idx_messages_type");
        builder.HasIndex(m => m.CreatedAt)
            .HasDatabaseName("idx_messages_created_at");
        builder.HasIndex(m => m.IsActive)
            .HasDatabaseName("idx_messages_is_active");
        builder.HasIndex(m => m.IsPinned)
            .HasDatabaseName("idx_messages_is_pinned")
            .HasFilter("is_pinned = true");
    }
}
