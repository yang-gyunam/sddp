using Sddp.Abstractions.ValueObjects;

namespace Sddp.Abstractions.Interfaces;

public interface IOutboxEnqueuer
{
    /// <summary>
    /// Serialises <paramref name="event"/> and persists it in the current transaction, so the row
    /// exists exactly when the change it describes does -- and disappears with it on rollback.
    /// </summary>
    /// <param name="event">Payload. Its type name becomes the outbox EventType.</param>
    /// <param name="aggregateType">What the event is about, e.g. "conversation".</param>
    /// <param name="aggregateId">Which one.</param>
    Task EnqueueAsync<TEvent>(
        TEvent @event,
        string aggregateType,
        GlobalUniqueId aggregateId,
        CancellationToken cancellationToken = default)
        where TEvent : class;
}
