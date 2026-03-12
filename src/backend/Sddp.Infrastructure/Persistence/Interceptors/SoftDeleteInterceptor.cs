using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Sddp.Abstractions.Base;

namespace Sddp.Infrastructure.Persistence.Interceptors;

/// <summary>
/// delete Interceptor
/// Delete IsActive = false change
/// </summary>
public class SoftDeleteInterceptor : SaveChangesInterceptor
{
    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result)
    {
        ConvertDeleteToSoftDelete(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        ConvertDeleteToSoftDelete(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private static void ConvertDeleteToSoftDelete(DbContext? context)
    {
        if (context is null) return;

        foreach (var entry in context.ChangeTracker.Entries<EntityBase>())
        {
            if (entry.State == EntityState.Deleted)
            {
                // delete delete
                entry.State = EntityState.Modified;
                entry.Entity.Deactivate();
            }
        }
    }
}
