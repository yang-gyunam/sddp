using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sddp.Abstractions.ValueObjects;
using Sddp.Domain.Entities;

namespace Sddp.Infrastructure.Persistence.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("users");

        builder.HasKey(u => u.Id);

        builder.Property(u => u.Id)
            .HasConversion(
                id => id.ToGuid(),
                guid => GlobalUniqueId.FromGuid(guid))
            .HasColumnName("id");

        builder.Property(u => u.Username)
            .HasColumnName("username")
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(u => u.Email)
            .HasColumnName("email")
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(u => u.PasswordHash)
            .HasColumnName("password_hash")
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(u => u.DisplayName)
            .HasColumnName("display_name")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(u => u.PersonId)
            .HasConversion(
                id => id.ToGuid(),
                guid => GlobalUniqueId.FromGuid(guid))
            .HasColumnName("person_id")
            .IsRequired();

        builder.Property(u => u.AvatarUrl)
            .HasColumnName("avatar_url")
            .HasMaxLength(500);

        builder.Property(u => u.IsActive)
            .HasColumnName("is_active")
            .HasDefaultValue(true);

        builder.Property(u => u.IsAI)
            .HasColumnName("is_ai")
            .HasDefaultValue(false);

        builder.Property(u => u.RequirePasswordChange)
            .HasColumnName("require_password_change")
            .HasDefaultValue(false);

        builder.Property(u => u.LastLoginAt)
            .HasConversion(
                ts => ts.HasValue ? ts.Value.ToDateTimeOffset() : (DateTimeOffset?)null,
                dto => dto.HasValue ? Timestamp.FromDateTimeOffset(dto.Value) : null)
            .HasColumnName("last_login_at");

        builder.Property(u => u.CreatedAt)
            .HasConversion(
                ts => ts.ToDateTimeOffset(),
                dto => Timestamp.FromDateTimeOffset(dto))
            .HasColumnName("created_at");

        builder.Property(u => u.UpdatedAt)
            .HasConversion(
                ts => ts.ToDateTimeOffset(),
                dto => Timestamp.FromDateTimeOffset(dto))
            .HasColumnName("updated_at");

        builder.Property(u => u.Preferences)
            .HasColumnName("preferences")
            .HasColumnType("jsonb");

        //
        builder.HasIndex(u => u.Username).IsUnique();
        builder.HasIndex(u => u.Email).IsUnique();
        builder.HasIndex(u => u.IsActive);
    }
}
