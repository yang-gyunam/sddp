using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sddp.Abstractions.Enums;
using Sddp.Abstractions.ValueObjects;
using Sddp.Domain.Entities;

namespace Sddp.Infrastructure.Persistence.Configurations;

public class DirectMessageConfiguration : IEntityTypeConfiguration<DirectMessage>
{
    public void Configure(EntityTypeBuilder<DirectMessage> builder)
    {
        builder.ToTable("direct_messages");

        builder.Property(d => d.Status)
            .HasColumnName("status")
            .HasMaxLength(20)
            .HasConversion(
                e => e.ToString(),
                s => Enum.Parse<DirectMessageStatus>(s))
            .HasDefaultValue(DirectMessageStatus.Active)
            .IsRequired();

        builder.Property(d => d.DecisionSpecId)
            .HasConversion(
                id => id.HasValue ? id.Value.ToGuid() : (Guid?)null,
                guid => guid.HasValue ? GlobalUniqueId.FromGuid(guid.Value) : null)
            .HasColumnName("decision_spec_id");
    }
}
