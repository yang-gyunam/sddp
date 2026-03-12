using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Sddp.Abstractions.Base;
using Sddp.Abstractions.ValueObjects;

namespace Sddp.Infrastructure.Persistence.Interceptors;

/// <summary>
/// settings Interceptor
/// ValidFrom/ValidTo settings
/// </summary>
public class VersioningInterceptor : SaveChangesInterceptor
{
    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result)
    {
        UpdateVersionedEntities(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        UpdateVersionedEntities(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private static void UpdateVersionedEntities(DbContext? context)
    {
        if (context is null) return;

        var now = Timestamp.Now;

        foreach (var entry in context.ChangeTracker.Entries<VersionedEntityBase>())
        {
            if (entry.State == EntityState.Added)
            {
                // new: ValidFrom settings ()
                var validFromProperty = entry.Property(nameof(VersionedEntityBase.ValidFrom));
                if (validFromProperty.CurrentValue is Timestamp validFrom && validFrom.IsEmpty)
                {
                    validFromProperty.CurrentValue = now;
                }

                // Version (SemanticVersion struct null)
                var versionProperty = entry.Property(nameof(VersionedEntityBase.Version));
                if (versionProperty.CurrentValue is SemanticVersion version && version.Equals(default))
                {
                    versionProperty.CurrentValue = SemanticVersion.Initial;
                }
            }
        }
    }
}
