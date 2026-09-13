using Sddp.Abstractions.Interfaces;
using Sddp.Abstractions.ValueObjects;

namespace Sddp.Api.Hubs;

/// <summary>
/// Thread-safe registry of server-side SignalR abort callbacks, covering every Hub.
/// </summary>
public sealed class UserConnectionRegistry : IUserConnectionRegistry
{
    private readonly Dictionary<GlobalUniqueId, Dictionary<string, Action>> _connections = [];
    private readonly Dictionary<string, GlobalUniqueId> _owners = new(StringComparer.Ordinal);
    private readonly object _sync = new();
    private readonly ILogger<UserConnectionRegistry> _logger;

    public UserConnectionRegistry(ILogger<UserConnectionRegistry> logger)
    {
        _logger = logger;
    }

    public void Register(GlobalUniqueId userId, string connectionId, Action abort)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionId);
        ArgumentNullException.ThrowIfNull(abort);

        lock (_sync)
        {
            RemoveLocked(connectionId);

            if (!_connections.TryGetValue(userId, out var userConnections))
            {
                userConnections = new Dictionary<string, Action>(StringComparer.Ordinal);
                _connections[userId] = userConnections;
            }

            userConnections[connectionId] = abort;
            _owners[connectionId] = userId;
        }
    }

    public void Unregister(string connectionId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionId);

        lock (_sync)
        {
            RemoveLocked(connectionId);
        }
    }

    public int AbortAll(GlobalUniqueId userId)
    {
        KeyValuePair<string, Action>[] callbacks;
        lock (_sync)
        {
            if (!_connections.Remove(userId, out var userConnections))
            {
                return 0;
            }

            callbacks = userConnections.ToArray();
            foreach (var connection in callbacks)
            {
                _owners.Remove(connection.Key);
            }
        }

        foreach (var (connectionId, abort) in callbacks)
        {
            try
            {
                abort();
            }
            catch (Exception exception)
            {
                // One broken callback must not leave the user's other sessions alive.
                _logger.LogError(
                    exception,
                    "Failed to abort SignalR connection {ConnectionId} for user {UserId}",
                    connectionId,
                    userId);
            }
        }

        return callbacks.Length;
    }

    public IReadOnlyList<string> GetConnectionIds(GlobalUniqueId userId)
    {
        lock (_sync)
        {
            return _connections.TryGetValue(userId, out var userConnections)
                ? userConnections.Keys.ToArray()
                : [];
        }
    }

    private void RemoveLocked(string connectionId)
    {
        if (!_owners.Remove(connectionId, out var owner))
        {
            return;
        }

        if (!_connections.TryGetValue(owner, out var userConnections))
        {
            return;
        }

        userConnections.Remove(connectionId);
        if (userConnections.Count == 0)
        {
            _connections.Remove(owner);
        }
    }
}
