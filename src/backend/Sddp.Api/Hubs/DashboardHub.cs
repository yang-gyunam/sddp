using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Sddp.Api.Hubs;

/// <summary>
/// Dashboard Hub
/// dashboard
/// </summary>
[Authorize]
public class DashboardHub : Hub
{
    private readonly ILogger<DashboardHub> _logger;

    public DashboardHub(ILogger<DashboardHub> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// dashboard
    /// </summary>
    /// <param name="view">dashboard: "my", "system", "project"</param>
    /// <param name="projectId">project dashboard project ID</param>
    public async Task SubscribeDashboard(string view, string? projectId = null)
    {
        var userId = GetUserId();
        if (userId is null)
        {
            throw new HubException("User not authenticated");
        }

        var groupName = GetGroupName(view, projectId, userId);
        await Groups.AddToGroupAsync(Context.ConnectionId, groupName);

        _logger.LogInformation(
            "User {UserId} subscribed to dashboard {View} (project: {ProjectId})",
            userId, view, projectId ?? "N/A");
    }

    /// <summary>
    /// dashboard
    /// </summary>
    public async Task UnsubscribeDashboard(string view, string? projectId = null)
    {
        var userId = GetUserId();
        if (userId is null)
        {
            throw new HubException("User not authenticated");
        }

        var groupName = GetGroupName(view, projectId, userId);
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);

        _logger.LogInformation(
            "User {UserId} unsubscribed from dashboard {View} (project: {ProjectId})",
            userId, view, projectId ?? "N/A");
    }

    /// <summary>
    ///
    /// </summary>
    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userId = GetUserId();
        _logger.LogInformation(
            "Dashboard user {UserId} disconnected. Exception: {Exception}",
            userId, exception?.Message);

        await base.OnDisconnectedAsync(exception);
    }

    #region Helper Methods

    private string? GetUserId()
    {
        return Context.User?.FindFirst("sub")?.Value
            ?? Context.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
    }

    private static string GetGroupName(string view, string? projectId, string userId)
    {
        return view.ToLowerInvariant() switch
        {
            "my" => $"dashboard:my:{userId}",
            "system" => "dashboard:system",
            "project" when !string.IsNullOrEmpty(projectId) => $"dashboard:project:{projectId}",
            _ => $"dashboard:{view}"
        };
    }

    #endregion
}

/// <summary>
/// DashboardHub (Typed Hub)
/// </summary>
public interface IDashboardHubClient
{
    Task StatisticUpdated(object payload);
    Task ActivityUpdated(object payload);
    Task SpecStatusChanged(object payload);
    Task NewNotification(object payload);
}
