using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sddp.Abstractions.Constants;
using Sddp.Abstractions.ValueObjects;
using Sddp.Domain.Entities;

namespace Sddp.Infrastructure.Persistence.Configurations;

public class ChannelConfiguration : IEntityTypeConfiguration<Channel>
{
    public void Configure(EntityTypeBuilder<Channel> builder)
    {
        builder.ToTable("channels");

        builder.Property(c => c.Status)
            .HasColumnName("status_code_id")
            .HasConversion(
                e => WellKnownCodes.ChannelStatusToId[e],
                id => WellKnownCodes.IdToChannelStatus[id])
            .IsRequired();

        builder.Property(c => c.DecisionSpecId)
            .HasConversion(
                id => id.HasValue ? id.Value.ToGuid() : (Guid?)null,
                guid => guid.HasValue ? GlobalUniqueId.FromGuid(guid.Value) : null)
            .HasColumnName("decision_spec_id");
    }
}
