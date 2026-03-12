using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sddp.Abstractions.ValueObjects;
using Sddp.Domain.Entities;

namespace Sddp.Infrastructure.Persistence.Configurations;

public class PersonConfiguration : IEntityTypeConfiguration<Person>
{
    public void Configure(EntityTypeBuilder<Person> builder)
    {
        builder.ToTable("persons");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Id)
            .HasConversion(
                id => id.ToGuid(),
                guid => GlobalUniqueId.FromGuid(guid))
            .HasColumnName("id");

        builder.Property(p => p.DisplayName)
            .HasColumnName("display_name")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(p => p.Email)
            .HasColumnName("email")
            .HasMaxLength(255);

        builder.Property(p => p.FirstName)
            .HasColumnName("first_name")
            .HasMaxLength(50);

        builder.Property(p => p.LastName)
            .HasColumnName("last_name")
            .HasMaxLength(50);

        builder.Property(p => p.AvatarUrl)
            .HasColumnName("avatar_url")
            .HasMaxLength(500);

        builder.Property(p => p.Organization)
            .HasColumnName("organization")
            .HasMaxLength(200);

        builder.Property(p => p.PersonType)
            .HasColumnName("person_type")
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(p => p.Timezone)
            .HasColumnName("timezone")
            .HasMaxLength(50);

        builder.Property(p => p.Locale)
            .HasColumnName("locale")
            .HasMaxLength(10);

        builder.Property(p => p.IsActive)
            .HasColumnName("is_active")
            .HasDefaultValue(true);

        builder.Property(p => p.CreatedAt)
            .HasConversion(
                ts => ts.ToDateTimeOffset(),
                dto => Timestamp.FromDateTimeOffset(dto))
            .HasColumnName("created_at");

        builder.Property(p => p.UpdatedAt)
            .HasConversion(
                ts => ts.ToDateTimeOffset(),
                dto => Timestamp.FromDateTimeOffset(dto))
            .HasColumnName("updated_at");
    }
}
