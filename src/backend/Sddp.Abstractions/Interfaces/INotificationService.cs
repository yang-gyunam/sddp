using Sddp.Abstractions.ValueObjects;

namespace Sddp.Abstractions.Interfaces;

/// <summary>
/// notification create
/// </summary>
public interface INotificationService
{
    Task CreateNotificationAsync(
        GlobalUniqueId tenantId,
        GlobalUniqueId recipientId,
        GlobalUniqueId? actorId,
        string type,
        string title,
        string message,
        string? entityType = null,
        GlobalUniqueId? entityId = null,
        CancellationToken ct = default);
}
