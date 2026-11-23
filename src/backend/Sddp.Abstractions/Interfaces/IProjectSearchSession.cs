using System.Data;

namespace Sddp.Abstractions.Interfaces;

/// <summary>Connection and transaction shared with the diagram origin permission checks.</summary>
public interface IProjectSearchSession
{
    IDbConnection Connection { get; }
    IDbTransaction? Transaction { get; }
}
