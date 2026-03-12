using System.Collections.Concurrent;
using Microsoft.EntityFrameworkCore.Storage;
using Sddp.Abstractions.Base;
using Sddp.Abstractions.Interfaces;
using Sddp.Infrastructure.Persistence.Contexts;
using Sddp.Infrastructure.Persistence.Repositories;

namespace Sddp.Infrastructure.Persistence;

/// <summary>
/// Unit of Work
/// </summary>
public class UnitOfWork : IUnitOfWork
{
    private readonly SddpDbContext _context;
    private readonly ConcurrentDictionary<Type, object> _repositories = new();
    private IDbContextTransaction? _transaction;
    private bool _saveChangesCalledInTransaction;
    private bool _disposed;

    public UnitOfWork(SddpDbContext context)
    {
        _context = context;
    }

    public IRepository<T> Repository<T>() where T : EntityBase
    {
        var type = typeof(T);

        if (_repositories.TryGetValue(type, out var existingRepository))
        {
            return (IRepository<T>)existingRepository;
        }

        var repository = new RepositoryBase<T>(_context);
        _repositories.TryAdd(type, repository);
        return repository;
    }

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        if (_transaction is not null)
        {
            _saveChangesCalledInTransaction = true;
        }

        return await (_context.SaveChangesAsync(cancellationToken)).ConfigureAwait(false);
    }

    public async Task<IDisposable> BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        _transaction = await (_context.Database.BeginTransactionAsync(cancellationToken)).ConfigureAwait(false);
        _saveChangesCalledInTransaction = false;
        return _transaction;
    }

    public async Task CommitAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            if (!_saveChangesCalledInTransaction)
            {
                await (_context.SaveChangesAsync(cancellationToken)).ConfigureAwait(false);
            }

            if (_transaction is not null)
            {
                await (_transaction.CommitAsync(cancellationToken)).ConfigureAwait(false);
            }
        }
        catch
        {
            await (RollbackAsync(cancellationToken)).ConfigureAwait(false);
            throw;
        }
        finally
        {
            if (_transaction is not null)
            {
                await (_transaction.DisposeAsync()).ConfigureAwait(false);
                _transaction = null;
            }

            _saveChangesCalledInTransaction = false;
        }
    }

    public async Task RollbackAsync(CancellationToken cancellationToken = default)
    {
        if (_transaction is not null)
        {
            await (_transaction.RollbackAsync(cancellationToken)).ConfigureAwait(false);
            await (_transaction.DisposeAsync()).ConfigureAwait(false);
            _transaction = null;
        }

        _saveChangesCalledInTransaction = false;
    }

    public async Task<T> ExecuteInTransactionAsync<T>(
        Func<CancellationToken, Task<T>> operation,
        CancellationToken cancellationToken = default)
    {
        var strategy = _context.Database.CreateExecutionStrategy();
        if (strategy.RetriesOnFailure)
        {
            // Npgsql retry strategy does not allow user-initiated transactions.
            // Execute the operation and persist changes without explicit BeginTransaction.
            var response = await (operation(cancellationToken)).ConfigureAwait(false);

            if (_transaction is null && _context.ChangeTracker.HasChanges())
            {
                await (_context.SaveChangesAsync(cancellationToken)).ConfigureAwait(false);
            }

            return response;
        }

        using var transaction = await (BeginTransactionAsync(cancellationToken)).ConfigureAwait(false);

        try
        {
            var response = await (operation(cancellationToken)).ConfigureAwait(false);
            await (CommitAsync(cancellationToken)).ConfigureAwait(false);
            return response;
        }
        catch
        {
            await (RollbackAsync(cancellationToken)).ConfigureAwait(false);
            throw;
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                _transaction?.Dispose();
                _context.Dispose();
            }

            _disposed = true;
        }
    }
}
