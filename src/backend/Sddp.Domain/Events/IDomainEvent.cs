using Sddp.Abstractions.ValueObjects;

namespace Sddp.Domain.Events;

/// <summary>
/// Domain event interface
/// </summary>
public interface IDomainEvent
{
    /// <summary>
    /// Event ID
    /// </summary>
    GlobalUniqueId EventId { get; }

    /// <summary>
    /// Time the event occurred
    /// </summary>
    Timestamp OccurredAt { get; }

    /// <summary>
    /// Event type name
    /// </summary>
    string EventType { get; }
}

/// <summary>
/// Base implementation of a domain event
/// </summary>
public abstract record DomainEventBase : IDomainEvent
{
    public GlobalUniqueId EventId { get; } = GlobalUniqueId.NewId();
    public Timestamp OccurredAt { get; } = Timestamp.Now;
    public virtual string EventType => GetType().Name;
}

/// <summary>
/// Domain event related to an aggregate root
/// </summary>
public abstract record AggregateEvent(GlobalUniqueId AggregateId, string AggregateType) : DomainEventBase;
