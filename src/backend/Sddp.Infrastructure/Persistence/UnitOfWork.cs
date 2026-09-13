using System.Collections.Concurrent;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Npgsql;
using Sddp.Abstractions.Base;
using Sddp.Abstractions.Exceptions;
using Sddp.Abstractions.Interfaces;
using Sddp.Domain.Entities;
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

        return await SaveChangesWithConcurrencyTranslationAsync(cancellationToken).ConfigureAwait(false);
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
                await SaveChangesWithConcurrencyTranslationAsync(cancellationToken).ConfigureAwait(false);
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
        // A nested command (a handler sending another command through MediatR) reaches here
        // with the outer boundary already open. Join it instead of starting a second one.
        if (_transaction is not null)
        {
            return await (operation(cancellationToken)).ConfigureAwait(false);
        }

        var attempted = false;

        return await (_context.Database.CreateExecutionStrategy().ExecuteAsync(
            async ct =>
            {
                // A failed attempt leaves its entities tracked as Added/Modified; replaying
                // over them would insert duplicates. Only on retry — clearing on the first
                // attempt would detach what the pipeline read before reaching this boundary.
                if (attempted)
                {
                    _context.ChangeTracker.Clear();
                }

                attempted = true;

                await (BeginTransactionAsync(ct)).ConfigureAwait(false);

                try
                {
                    var response = await (operation(ct)).ConfigureAwait(false);
                    await (CommitAsync(ct)).ConfigureAwait(false);
                    return response;
                }
                catch
                {
                    await (RollbackAsync(ct)).ConfigureAwait(false);
                    throw;
                }
            },
            cancellationToken)).ConfigureAwait(false);
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

    private async Task<int> SaveChangesWithConcurrencyTranslationAsync(
        CancellationToken cancellationToken)
    {
        try
        {
            return await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (DbUpdateConcurrencyException exception)
        {
            var pageEntry = exception.Entries.FirstOrDefault(
                entry => entry.Entity is KnowledgePage);
            if (pageEntry is null)
            {
                throw;
            }

            var expectedRevision = pageEntry.OriginalValues.GetValue<int>(
                nameof(KnowledgePage.Revision));
            var databaseValues = await pageEntry.GetDatabaseValuesAsync(cancellationToken)
                .ConfigureAwait(false);
            var currentRevision = databaseValues?.GetValue<int>(
                nameof(KnowledgePage.Revision)) ?? expectedRevision;

            throw new KnowledgePageEditConflictException(
                expectedRevision,
                currentRevision);
        }
        catch (DbUpdateException exception)
            when (TryGetUniqueViolationConstraint(exception, out var constraintName))
        {
            // Slug allocation reads siblings before writing, so two concurrent creates under the
            // same parent can both pass the pre-check and collide on the partial unique index.
            // Translate that race into the same 409 the pre-check produces instead of a 500.
            throw KnowledgeSlugConstraints.Contains(constraintName)
                ? new ConflictException(
                    "That page slug was just taken under the selected parent. Retry the request.")
                : new ConflictException(
                    "The change conflicts with an existing record. Retry the request.");
        }
    }

    private static readonly HashSet<string> KnowledgeSlugConstraints = new(StringComparer.Ordinal)
    {
        "uq_knowledge_pages_root_slug",
        "uq_knowledge_pages_child_slug"
    };

    /// <summary>
    /// Detects a PostgreSQL unique-violation (SQLSTATE 23505) and returns its constraint name.
    /// </summary>
    private static bool TryGetUniqueViolationConstraint(
        DbUpdateException exception,
        out string constraintName)
    {
        constraintName = string.Empty;
        if (exception.InnerException is not PostgresException
            {
                SqlState: PostgresErrorCodes.UniqueViolation
            } postgresException)
        {
            return false;
        }

        constraintName = postgresException.ConstraintName ?? string.Empty;
        return true;
    }
}
