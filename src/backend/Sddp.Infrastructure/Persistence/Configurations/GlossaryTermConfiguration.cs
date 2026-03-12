using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sddp.Abstractions.Constants;
using Sddp.Abstractions.ValueObjects;
using Sddp.Domain.Entities;

namespace Sddp.Infrastructure.Persistence.Configurations;

public class GlossaryTermConfiguration : IEntityTypeConfiguration<GlossaryTerm>
{
    public void Configure(EntityTypeBuilder<GlossaryTerm> builder)
    {
        builder.ToTable("glossary_terms");

        builder.HasKey(g => g.Id);

        builder.Property(g => g.Id)
            .HasConversion(
                id => id.ToGuid(),
                guid => GlobalUniqueId.FromGuid(guid))
            .HasColumnName("id");

        builder.Property(g => g.TenantId)
            .HasConversion(
                id => id.ToGuid(),
                guid => GlobalUniqueId.FromGuid(guid))
            .HasColumnName("tenant_id")
            .IsRequired();

        builder.Property(g => g.ProjectId)
            .HasConversion(
                id => id.ToGuid(),
                guid => GlobalUniqueId.FromGuid(guid))
            .HasColumnName("project_id")
            .IsRequired();

        builder.Property(g => g.Term)
            .HasColumnName("term")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(g => g.Definition)
            .HasColumnName("definition")
            .HasColumnType("text")
            .IsRequired();

        builder.Property(g => g.Category)
            .HasColumnName("category_code_id")
            .HasConversion(
                e => WellKnownCodes.TermCategoryToId[e],
                id => WellKnownCodes.IdToTermCategory[id])
            .IsRequired();

        // JSON
        builder.Property(g => g.UsageExamples)
            .HasColumnName("usage_examples")
            .HasColumnType("jsonb");

        // JSON (GlobalUniqueId JSON)
        builder.Property(g => g.RelatedTermIds)
            .HasConversion(
                ids => JsonSerializer.Serialize(ids.Select(id => id.ToGuid()).ToList(), (JsonSerializerOptions?)null),
                json => string.IsNullOrEmpty(json)
                    ? new List<GlobalUniqueId>()
                    : JsonSerializer.Deserialize<List<Guid>>(json, (JsonSerializerOptions?)null)!
                        .Select(guid => GlobalUniqueId.FromGuid(guid))
                        .ToList())
            .HasColumnName("related_term_ids")
            .HasColumnType("jsonb");

        builder.Property(g => g.Source)
            .HasColumnName("source")
            .HasMaxLength(500);

        builder.Property(g => g.DefinedBy)
            .HasConversion(
                id => id.ToGuid(),
                guid => GlobalUniqueId.FromGuid(guid))
            .HasColumnName("defined_by")
            .IsRequired();

        builder.Property(g => g.OwnerUserId)
            .HasConversion(
                id => id.HasValue ? id.Value.ToGuid() : (Guid?)null,
                guid => guid.HasValue ? GlobalUniqueId.FromGuid(guid.Value) : null)
            .HasColumnName("owner_user_id");

        builder.Property(g => g.ApprovedBy)
            .HasConversion(
                id => id.HasValue ? id.Value.ToGuid() : (Guid?)null,
                guid => guid.HasValue ? GlobalUniqueId.FromGuid(guid.Value) : null)
            .HasColumnName("approved_by");

        builder.Property(g => g.ApprovedAt)
            .HasConversion(
                ts => ts.HasValue ? ts.Value.ToDateTimeOffset() : (DateTimeOffset?)null,
                dto => dto.HasValue ? Timestamp.FromDateTimeOffset(dto.Value) : null)
            .HasColumnName("approved_at");

        builder.Property(g => g.Status)
            .HasColumnName("status_code_id")
            .HasConversion(
                e => WellKnownCodes.GlossaryTermStatusToId[e],
                id => WellKnownCodes.IdToGlossaryTermStatus[id])
            .IsRequired();

        builder.Property(g => g.SourceSpecId)
            .HasConversion(
                id => id.HasValue ? id.Value.ToGuid() : (Guid?)null,
                guid => guid.HasValue ? GlobalUniqueId.FromGuid(guid.Value) : null)
            .HasColumnName("source_spec_id");

        builder.Property(g => g.SourceConversationId)
            .HasConversion(
                id => id.HasValue ? id.Value.ToGuid() : (Guid?)null,
                guid => guid.HasValue ? GlobalUniqueId.FromGuid(guid.Value) : null)
            .HasColumnName("source_conversation_id");

        builder.Property(g => g.SourceRequirementId)
            .HasConversion(
                id => id.HasValue ? id.Value.ToGuid() : (Guid?)null,
                guid => guid.HasValue ? GlobalUniqueId.FromGuid(guid.Value) : null)
            .HasColumnName("source_requirement_id");

        builder.Property(g => g.ReplacedByTermId)
            .HasConversion(
                id => id.HasValue ? id.Value.ToGuid() : (Guid?)null,
                guid => guid.HasValue ? GlobalUniqueId.FromGuid(guid.Value) : null)
            .HasColumnName("replaced_by_term_id");

        builder.Property(g => g.Synonyms)
            .HasColumnName("synonyms")
            .HasMaxLength(500);

        builder.Property(g => g.Abbreviation)
            .HasColumnName("abbreviation")
            .HasMaxLength(50);

        // VersionedEntityBase
        builder.Property(g => g.Version)
            .HasConversion(
                v => v.ToString(),
                str => SemanticVersion.Parse(str))
            .HasColumnName("version")
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(g => g.ValidFrom)
            .HasConversion(
                ts => ts.ToDateTimeOffset(),
                dto => Timestamp.FromDateTimeOffset(dto))
            .HasColumnName("valid_from");

        builder.Property(g => g.ValidTo)
            .HasConversion(
                ts => ts.HasValue ? ts.Value.ToDateTimeOffset() : (DateTimeOffset?)null,
                dto => dto.HasValue ? Timestamp.FromDateTimeOffset(dto.Value) : null)
            .HasColumnName("valid_to");

        // AuditableEntityBase
        builder.Property(g => g.CreatedBy)
            .HasConversion(
                id => id.ToGuid(),
                guid => GlobalUniqueId.FromGuid(guid))
            .HasColumnName("created_by");

        builder.Property(g => g.UpdatedBy)
            .HasConversion(
                id => id.ToGuid(),
                guid => GlobalUniqueId.FromGuid(guid))
            .HasColumnName("updated_by");

        // EntityBase
        builder.Property(g => g.IsActive)
            .HasColumnName("is_active")
            .HasDefaultValue(true);

        builder.Property(g => g.CreatedAt)
            .HasConversion(
                ts => ts.ToDateTimeOffset(),
                dto => Timestamp.FromDateTimeOffset(dto))
            .HasColumnName("created_at");

        builder.Property(g => g.UpdatedAt)
            .HasConversion(
                ts => ts.ToDateTimeOffset(),
                dto => Timestamp.FromDateTimeOffset(dto))
            .HasColumnName("updated_at");

        // relationship: SourceSpec
        builder.HasOne<Spec>()
            .WithMany()
            .HasForeignKey(g => g.SourceSpecId)
            .OnDelete(DeleteBehavior.SetNull);

        // relationship: SourceConversation
        builder.HasOne<Conversation>()
            .WithMany()
            .HasForeignKey(g => g.SourceConversationId)
            .OnDelete(DeleteBehavior.SetNull);

        // relationship: SourceRequirement
        builder.HasOne<Requirement>()
            .WithMany()
            .HasForeignKey(g => g.SourceRequirementId)
            .OnDelete(DeleteBehavior.SetNull);

        // relationship: ReplacedByTerm (self-referencing)
        builder.HasOne(g => g.ReplacedByTerm)
            .WithMany()
            .HasForeignKey(g => g.ReplacedByTermId)
            .OnDelete(DeleteBehavior.SetNull);

        //
        builder.HasIndex(g => new { g.TenantId, g.ProjectId, g.Term })
            .IsUnique()
.HasFilter("valid_to IS NULL") // glossary 
            .HasDatabaseName("ix_glossary_terms_tenant_project_term_active");

        builder.HasIndex(g => g.Status)
            .HasDatabaseName("ix_glossary_terms_status");

        builder.HasIndex(g => g.Category)
            .HasDatabaseName("ix_glossary_terms_category");

        builder.HasIndex(g => new { g.TenantId, g.ProjectId })
            .HasDatabaseName("ix_glossary_terms_tenant_project");

        builder.HasIndex(g => g.IsActive)
            .HasDatabaseName("ix_glossary_terms_is_active");

        builder.HasIndex(g => g.DefinedBy)
            .HasDatabaseName("ix_glossary_terms_defined_by");

        builder.HasIndex(g => g.SourceSpecId)
            .HasDatabaseName("ix_glossary_terms_source_spec_id");

        builder.HasIndex(g => g.SourceConversationId)
            .HasDatabaseName("ix_glossary_terms_source_conversation_id");

        builder.HasIndex(g => g.SourceRequirementId)
            .HasDatabaseName("ix_glossary_terms_source_requirement_id");

        builder.HasIndex(g => g.OwnerUserId)
            .HasDatabaseName("ix_glossary_terms_owner_user_id");

        // search GIN (PostgreSQL)
        builder.HasIndex(g => g.Term)
            .HasDatabaseName("ix_glossary_terms_term_gin")
            .HasMethod("gin")
            .HasOperators("gin_trgm_ops");

        builder.HasIndex(g => g.Definition)
            .HasDatabaseName("ix_glossary_terms_definition_gin")
            .HasMethod("gin")
            .HasOperators("gin_trgm_ops");
    }
}
