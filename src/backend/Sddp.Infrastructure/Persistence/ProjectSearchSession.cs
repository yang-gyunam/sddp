using System.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Sddp.Abstractions.Interfaces;
using Sddp.Infrastructure.Persistence.Contexts;

namespace Sddp.Infrastructure.Persistence;

public sealed class ProjectSearchSession(SddpDbContext db) : IProjectSearchSession
{
    public IDbConnection Connection => db.Database.GetDbConnection();
    public IDbTransaction? Transaction => db.Database.CurrentTransaction?.GetDbTransaction();
}
