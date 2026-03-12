using Sddp.Abstractions.Interfaces;

namespace Sddp.Api.Hubs;

public class PresenceTracker : IPresenceTracker
{
    private readonly Dictionary<string, HashSet<string>> _onlineUsers = new();
    private readonly object _lock = new();

    public bool UserConnected(string userId, string connectionId)
    {
        lock (_lock)
        {
            if (_onlineUsers.TryGetValue(userId, out var connections))
            {
                connections.Add(connectionId);
                return false; // Already online
            }

            _onlineUsers[userId] = new HashSet<string> { connectionId };
            return true; // Just came online
        }
    }

    public bool UserDisconnected(string userId, string connectionId)
    {
        lock (_lock)
        {
            if (!_onlineUsers.TryGetValue(userId, out var connections))
            {
                return false; // Not tracked
            }

            connections.Remove(connectionId);

            if (connections.Count == 0)
            {
                _onlineUsers.Remove(userId);
                return true; // Went offline
            }

            return false; // Still has other connections
        }
    }

    public string[] GetOnlineUsers()
    {
        lock (_lock)
        {
            return _onlineUsers.Keys.ToArray();
        }
    }

    public IReadOnlyList<string> GetConnectionIds(string userId)
    {
        lock (_lock)
        {
            if (_onlineUsers.TryGetValue(userId, out var connections))
            {
                return connections.ToList();
            }

            return Array.Empty<string>();
        }
    }
}
