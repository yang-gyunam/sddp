using System.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Sddp.Application.Features.Relationships.Queries;
using Sddp.Infrastructure.Persistence.Contexts;

namespace Sddp.Api.Authorization;

/// <summary>Each relationship traversal checks its candidates and endpoints against one database snapshot.</summary>
public sealed class RelationshipReadSnapshot<TRequest, TResponse>(SddpDbContext db, DiagramRelationshipAccess access) : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRelationshipReadQuery
{
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken ct)
    {
        if (!db.Database.IsRelational() || db.Database.CurrentTransaction is not null) return await next();
        var attempted = false;
        return await db.Database.CreateExecutionStrategy().ExecuteAsync(async token =>
        {
            if (attempted)
            {
                // A replay takes a new snapshot, including current roles and memberships.
                db.ChangeTracker.Clear();
                access.ClearCachedAccess();
            }
            attempted = true;
            await using var snapshot = await db.Database.BeginTransactionAsync(IsolationLevel.RepeatableRead, token);
            var result = await next();
            await snapshot.CommitAsync(token);
            return result;
        }, ct);
    }
}
