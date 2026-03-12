using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sddp.Abstractions.ValueObjects;
using Sddp.Domain.Entities;

namespace Sddp.Infrastructure.Persistence.Configurations;

public class ProjectMemberConfiguration : IEntityTypeConfiguration<ProjectMember>
{
    public void Configure(EntityTypeBuilder<ProjectMember> builder)
    {
        builder.ToTable("project_members");

        builder.HasKey(pm => pm.Id);

        builder.Property(pm => pm.Id)
            .HasConversion(
                id => id.ToGuid(),
                guid => GlobalUniqueId.FromGuid(guid))
            .HasColumnName("id");

        builder.Property(pm => pm.ProjectId)
            .HasConversion(
                id => id.ToGuid(),
                guid => GlobalUniqueId.FromGuid(guid))
            .HasColumnName("project_id")
            .IsRequired();

        builder.Property(pm => pm.UserId)
            .HasConversion(
                id => id.ToGuid(),
                guid => GlobalUniqueId.FromGuid(guid))
            .HasColumnName("user_id")
            .IsRequired();

        builder.Property(pm => pm.UserRoleId)
            .HasConversion(
                id => id.ToGuid(),
                guid => GlobalUniqueId.FromGuid(guid))
            .HasColumnName("user_role_id")
            .IsRequired();

        builder.Property(pm => pm.JoinedAt)
            .HasConversion(
                ts => ts.ToDateTimeOffset(),
                dto => Timestamp.FromDateTimeOffset(dto))
            .HasColumnName("joined_at");

        builder.Property(pm => pm.IsActive)
            .HasColumnName("is_active")
            .HasDefaultValue(true);

        builder.Property(pm => pm.CreatedAt)
            .HasConversion(
                ts => ts.ToDateTimeOffset(),
                dto => Timestamp.FromDateTimeOffset(dto))
            .HasColumnName("created_at");

        builder.Property(pm => pm.UpdatedAt)
            .HasConversion(
                ts => ts.ToDateTimeOffset(),
                dto => Timestamp.FromDateTimeOffset(dto))
            .HasColumnName("updated_at");

        // relationship settings
        builder.HasOne(pm => pm.Project)
            .WithMany(p => p.Members)
            .HasForeignKey(pm => pm.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(pm => pm.User)
            .WithMany()
            .HasForeignKey(pm => pm.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(pm => pm.UserRole)
            .WithMany()
            .HasForeignKey(pm => pm.UserRoleId)
            .OnDelete(DeleteBehavior.Cascade);

        //
        builder.HasIndex(pm => new { pm.ProjectId, pm.UserId })
            .IsUnique()
            .HasFilter("\"is_active\" = true");
        builder.HasIndex(pm => pm.UserRoleId).IsUnique();
    }
}
