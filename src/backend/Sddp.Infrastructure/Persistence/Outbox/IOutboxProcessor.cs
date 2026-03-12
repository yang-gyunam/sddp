namespace Sddp.Infrastructure.Persistence.Outbox;

/// <summary>
/// Outbox message
/// </summary>
public interface IOutboxProcessor
{
    /// <summary>
    /// message
    /// </summary>
    Task ProcessPendingMessagesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// message
    /// </summary>
    Task RetryFailedMessagesAsync(CancellationToken cancellationToken = default);
}
