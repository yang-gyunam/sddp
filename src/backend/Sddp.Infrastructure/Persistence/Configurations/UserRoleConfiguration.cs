using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sddp.Abstractions.ValueObjects;
using Sddp.Domain.Entities;

namespace Sddp.Infrastructure.Persistence.Configurations;

public class UserRoleConfiguration : IEntityTypeConfiguration<UserRole>
{
    public void Configure(EntityTypeBuilder<UserRole> builder)
    {
        builder.ToTable("user_roles");

        builder.HasKey(ur => ur.Id);

        builder.Property(ur => ur.Id)
            .HasConversion(
                id => id.ToGuid(),
                guid => GlobalUniqueId.FromGuid(guid))
            .HasColumnName("id");

        builder.Property(ur => ur.UserId)
            .HasConversion(
                id => id.ToGuid(),
                guid => GlobalUniqueId.FromGuid(guid))
            .HasColumnName("user_id")
            .IsRequired();

        builder.Property(ur => ur.RoleId)
            .HasConversion(
                id => id.ToGuid(),
                guid => GlobalUniqueId.FromGuid(guid))
            .HasColumnName("role_id")
            .IsRequired();

        builder.Property(ur => ur.AssignedBy)
            .HasConversion(
                id => id.ToGuid(),
                guid => GlobalUniqueId.FromGuid(guid))
            .HasColumnName("assigned_by")
            .IsRequired();

        builder.Property(ur => ur.AssignedAt)
            .HasConversion(
                ts => ts.ToDateTimeOffset(),
                dto => Timestamp.FromDateTimeOffset(dto))
            .HasColumnName("assigned_at");

        builder.Property(ur => ur.TenantId)
            .HasConversion(
                id => id == null ? (Guid?)null : id.Value.ToGuid(),
                guid => guid == null ? null : GlobalUniqueId.FromGuid(guid.Value))
            .HasColumnName("tenant_id");

        builder.Property(ur => ur.ProjectId)
            .HasConversion(
                id => id == null ? (Guid?)null : id.Value.ToGuid(),
                guid => guid == null ? null : GlobalUniqueId.FromGuid(guid.Value))
            .HasColumnName("project_id");

        builder.Property(ur => ur.IsActive)
            .HasColumnName("is_active")
            .HasDefaultValue(true);

        builder.Property(ur => ur.CreatedAt)
            .HasConversion(
                ts => ts.ToDateTimeOffset(),
                dto => Timestamp.FromDateTimeOffset(dto))
            .HasColumnName("created_at");

        builder.Property(ur => ur.UpdatedAt)
            .HasConversion(
                ts => ts.ToDateTimeOffset(),
                dto => Timestamp.FromDateTimeOffset(dto))
            .HasColumnName("updated_at");

        // relationship settings
        builder.HasOne(ur => ur.User)
            .WithMany(u => u.UserRoles)
            .HasForeignKey(ur => ur.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(ur => ur.Role)
            .WithMany(r => r.UserRoles)
            .HasForeignKey(ur => ur.RoleId)
            .OnDelete(DeleteBehavior.Cascade);

        // (user role+tenant+project)
        builder.HasIndex(ur => new { ur.UserId, ur.RoleId, ur.TenantId, ur.ProjectId }).IsUnique();
    }
}
