using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Sddp.Abstractions.Base;
using Sddp.Abstractions.Interfaces;
using Sddp.Abstractions.ValueObjects;
using Sddp.Infrastructure.Persistence.Contexts;

namespace Sddp.Infrastructure.Persistence.Repositories;

/// <summary>
/// Command Side Repository default (EF Core)
/// </summary>
public class RepositoryBase<T> : IRepository<T> where T : EntityBase
{
    protected readonly SddpDbContext Context;
    protected readonly DbSet<T> DbSet;

    public RepositoryBase(SddpDbContext context)
    {
        Context = context;
        DbSet = context.Set<T>();
    }

    public virtual async Task<T?> GetByIdAsync(GlobalUniqueId id, CancellationToken cancellationToken = default)
    {
        return await (DbSet
            .Where(e => e.IsActive)
            .FirstOrDefaultAsync(e => e.Id == id, cancellationToken)).ConfigureAwait(false);
    }

    public virtual async Task<T?> GetByIdIncludingInactiveAsync(GlobalUniqueId id, CancellationToken cancellationToken = default)
    {
        return await (DbSet
            .FirstOrDefaultAsync(e => e.Id == id, cancellationToken)).ConfigureAwait(false);
    }

    public virtual async Task<IReadOnlyList<T>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await (DbSet
            .Where(e => e.IsActive)
            .ToListAsync(cancellationToken)).ConfigureAwait(false);
    }

    public virtual async Task<IReadOnlyList<T>> GetAllIncludingInactiveAsync(CancellationToken cancellationToken = default)
    {
        return await (DbSet
            .ToListAsync(cancellationToken)).ConfigureAwait(false);
    }

    public virtual async Task<IReadOnlyList<T>> FindAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default)
    {
        return await (DbSet
            .Where(e => e.IsActive)
            .Where(predicate)
            .ToListAsync(cancellationToken)).ConfigureAwait(false);
    }

    public virtual async Task<IReadOnlyList<T>> FindIncludingInactiveAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default)
    {
        return await (DbSet
            .Where(predicate)
            .ToListAsync(cancellationToken)).ConfigureAwait(false);
    }

    public virtual async Task<(IReadOnlyList<T> Items, int TotalCount)> FindPagedAsync(
        Expression<Func<T, bool>> predicate,
        int page,
        int pageSize,
        Expression<Func<T, object>> orderBy,
        bool descending = false,
        CancellationToken cancellationToken = default)
    {
        var query = DbSet.Where(e => e.IsActive).Where(predicate);
        var totalCount = await (query.CountAsync(cancellationToken)).ConfigureAwait(false);
        var ordered = descending ? query.OrderByDescending(orderBy) : query.OrderBy(orderBy);
        var items = await (ordered
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken)).ConfigureAwait(false);
        return (items, totalCount);
    }

    public virtual async Task<(IReadOnlyList<T> Items, int TotalCount)> FindPagedAsync(
        Expression<Func<T, bool>> predicate,
        int page,
        int pageSize,
        Func<IQueryable<T>, IOrderedQueryable<T>> orderByFactory,
        CancellationToken cancellationToken = default)
    {
        var query = DbSet.Where(e => e.IsActive).Where(predicate);
        var totalCount = await (query.CountAsync(cancellationToken)).ConfigureAwait(false);
        var ordered = orderByFactory(query);
        var items = await (ordered
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken)).ConfigureAwait(false);
        return (items, totalCount);
    }

    public virtual async Task<T> AddAsync(T entity, CancellationToken cancellationToken = default)
    {
        await (DbSet.AddAsync(entity, cancellationToken)).ConfigureAwait(false);
        return entity;
    }

    public virtual Task UpdateAsync(T entity, CancellationToken cancellationToken = default)
    {
        DbSet.Update(entity);
        return Task.CompletedTask;
    }

    public virtual Task DeleteAsync(T entity, CancellationToken cancellationToken = default)
    {
        // SoftDeleteInterceptor delete delete
        DbSet.Remove(entity);
        return Task.CompletedTask;
    }

    public virtual async Task<bool> ExistsAsync(GlobalUniqueId id, CancellationToken cancellationToken = default)
    {
        return await (DbSet
            .Where(e => e.IsActive)
            .AnyAsync(e => e.Id == id, cancellationToken)).ConfigureAwait(false);
    }

    public virtual async Task<int> CountAsync(CancellationToken cancellationToken = default)
    {
        return await (DbSet
            .Where(e => e.IsActive)
            .CountAsync(cancellationToken)).ConfigureAwait(false);
    }

    public virtual async Task<int> CountAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default)
    {
        return await (DbSet
            .Where(e => e.IsActive)
            .Where(predicate)
            .CountAsync(cancellationToken)).ConfigureAwait(false);
    }
}
