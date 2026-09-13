using Sddp.Abstractions.ValueObjects;

namespace Sddp.Abstractions.Interfaces;

/// <summary>
/// Tracks live SignalR connections that must be closed when an account is deactivated.
/// This is deliberately separate from conversation presence because it covers every Hub and
/// retains a server-side abort capability, not only connection IDs.
/// </summary>
public interface IUserConnectionRegistry
{
    void Register(GlobalUniqueId userId, string connectionId, Action abort);

    void Unregister(string connectionId);

    /// <summary>
    /// Removes and aborts every connection currently owned by <paramref name="userId"/>.
    /// </summary>
    int AbortAll(GlobalUniqueId userId);

    IReadOnlyList<string> GetConnectionIds(GlobalUniqueId userId);
}
