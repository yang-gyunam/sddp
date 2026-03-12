using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sddp.Domain.Entities;

namespace Sddp.Infrastructure.Persistence.Configurations;

public class ForumConfiguration : IEntityTypeConfiguration<Forum>
{
    public void Configure(EntityTypeBuilder<Forum> builder)
    {
        builder.ToTable("forums");

        // relationship - Topics
        builder.HasMany(f => f.Topics)
            .WithOne(t => t.Forum)
            .HasForeignKey(t => t.ForumId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
