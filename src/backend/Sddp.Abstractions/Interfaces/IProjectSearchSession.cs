using System.Data;
using Sddp.Abstractions.DTOs;
using Sddp.Abstractions.ValueObjects;

namespace Sddp.Abstractions.Interfaces;

/// <summary>Connection and transaction shared with the diagram origin permission checks.</summary>
public interface IProjectSearchSession
{
    IDbConnection Connection { get; }
    IDbTransaction? Transaction { get; }
}

/// <summary>The optional AI module contributes authorized private chats to project search.</summary>
public interface IProjectSearchChatSource
{
    Task<IReadOnlyList<ProjectSearchChatDocument>?> ReadAsync(IProjectSearchSession session, GlobalUniqueId tenant,
        GlobalUniqueId project, GlobalUniqueId user, string[] terms, CancellationToken ct);
}
