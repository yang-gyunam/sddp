namespace Sddp.Abstractions.Interfaces;

/// <summary>
/// Tracks user online/offline presence via SignalR connections.
/// Thread-safe singleton that maps userId → connectionIds (supports multi-tab).
/// </summary>
public interface IPresenceTracker
{
    /// <summary>
    /// Register a user connection. Returns true if this is the user's first connection (went online).
    /// </summary>
    bool UserConnected(string userId, string connectionId);

    /// <summary>
    /// Remove a user connection. Returns true if this was the user's last connection (went offline).
    /// </summary>
    bool UserDisconnected(string userId, string connectionId);

    /// <summary>
    /// Get all currently online user IDs.
    /// </summary>
    string[] GetOnlineUsers();

    /// <summary>
    /// Get all connection IDs for a specific user (supports multi-tab).
    /// Returns empty list if user is not online.
    /// </summary>
    IReadOnlyList<string> GetConnectionIds(string userId);
}
