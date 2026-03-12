using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Sddp.Abstractions.Base;
using Sddp.Abstractions.ValueObjects;

namespace Sddp.Infrastructure.Persistence.Interceptors;

/// <summary>
/// audit settings Interceptor
/// CreatedAt, UpdatedAt settings
/// </summary>
public class AuditableEntityInterceptor : SaveChangesInterceptor
{
    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result)
    {
        UpdateEntities(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        UpdateEntities(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private static void UpdateEntities(DbContext? context)
    {
        if (context is null) return;

        var now = Timestamp.Now;

        foreach (var entry in context.ChangeTracker.Entries<EntityBase>())
        {
            if (entry.State == EntityState.Added)
            {
                // new create entity: ID settings
                if (entry.Entity.Id.IsEmpty)
                {
                    SetIfMapped(entry, nameof(EntityBase.Id), GlobalUniqueId.NewId());
                }
                SetIfMapped(entry, nameof(EntityBase.CreatedAt), now);
                SetIfMapped(entry, nameof(EntityBase.UpdatedAt), now);
            }
            else if (entry.State == EntityState.Modified)
            {
                // update entity: UpdatedAt
                SetIfMapped(entry, nameof(EntityBase.UpdatedAt), now);
            }
        }
    }

    private static void SetIfMapped(
        Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry<EntityBase> entry,
        string propertyName,
        object value)
    {
        if (entry.Metadata.FindProperty(propertyName) is null)
        {
            return;
        }

        entry.Property(propertyName).CurrentValue = value;
    }
}
