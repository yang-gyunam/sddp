using Sddp.Domain.Events;

namespace Sddp.Infrastructure.Persistence.Projections;

/// <summary>
/// Projection
/// </summary>
/// <typeparam name="TEvent"> domain </typeparam>
public interface IProjectionHandler<in TEvent> where TEvent : IDomainEvent
{
    /// <summary>
    /// Projection
    /// </summary>
    Task HandleAsync(TEvent @event, CancellationToken cancellationToken = default);
}

/// <summary>
/// Projection
/// </summary>
public interface IProjectionDispatcher
{
    /// <summary>
    /// Projection
    /// </summary>
    Task DispatchAsync(string eventType, string payload, CancellationToken cancellationToken = default);
}
