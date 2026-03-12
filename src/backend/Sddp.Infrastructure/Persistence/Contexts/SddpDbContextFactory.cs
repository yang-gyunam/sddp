using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Sddp.Infrastructure.Persistence.Contexts;

public class SddpDbContextFactory : IDesignTimeDbContextFactory<SddpDbContext>
{
    public SddpDbContext CreateDbContext(string[] args)
    {
        var connectionString =
            Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
            ?? "Host=localhost;Port=5432;Database=sddp;Username=sddp;Password=sddp_password";

        var optionsBuilder = new DbContextOptionsBuilder<SddpDbContext>();
        optionsBuilder.UseNpgsql(connectionString, npgsqlOptions =>
        {
            npgsqlOptions.MigrationsAssembly(typeof(SddpDbContext).Assembly.FullName);
            npgsqlOptions.EnableRetryOnFailure(3);
        });

        return new SddpDbContext(optionsBuilder.Options);
    }
}
