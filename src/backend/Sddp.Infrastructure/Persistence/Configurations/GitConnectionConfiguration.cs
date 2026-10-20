using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sddp.Abstractions.ValueObjects;
using Sddp.Domain.Entities;
namespace Sddp.Infrastructure.Persistence.Configurations;
public sealed class GitConnectionConfiguration : IEntityTypeConfiguration<GitConnection>
{
    public void Configure(EntityTypeBuilder<GitConnection> b)
    {
        b.ToTable("git_connections"); b.HasKey(x => x.Id);
        b.Property(x => x.Id).HasColumnName("id").HasConversion(x => x.ToGuid(), x => GlobalUniqueId.FromGuid(x));
        b.Property(x => x.TenantId).HasColumnName("tenant_id").HasConversion(x => x.ToGuid(), x => GlobalUniqueId.FromGuid(x));
        b.Property(x => x.ProjectId).HasColumnName("project_id").HasConversion(x => x.ToGuid(), x => GlobalUniqueId.FromGuid(x));
        b.Property(x => x.Name).HasColumnName("name").HasMaxLength(100);
        b.Property(x => x.Provider).HasColumnName("provider").HasMaxLength(20);
        b.Property(x => x.ServerUrl).HasColumnName("server_url").HasMaxLength(2048);
        b.Property(x => x.Username).HasColumnName("username").HasMaxLength(200);
        b.Property(x => x.ProtectedToken).HasColumnName("protected_token");
        b.Property(x => x.Version).HasColumnName("version").IsConcurrencyToken();
        b.Property(x => x.CheckedAt).HasColumnName("checked_at");
        b.Property(x => x.IsActive).HasColumnName("is_active");
        b.Property(x => x.CreatedAt).HasColumnName("created_at").HasConversion(x => x.ToDateTimeOffset(), x => Timestamp.FromDateTimeOffset(x));
        b.Property(x => x.UpdatedAt).HasColumnName("updated_at").HasConversion(x => x.ToDateTimeOffset(), x => Timestamp.FromDateTimeOffset(x));
        b.HasOne<Project>().WithMany().HasForeignKey(x => new {x.TenantId, x.ProjectId}).HasPrincipalKey(x => new {x.TenantId,x.Id}).OnDelete(DeleteBehavior.Restrict);
    }
}
