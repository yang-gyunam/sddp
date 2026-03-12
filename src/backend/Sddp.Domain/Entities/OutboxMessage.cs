using Sddp.Abstractions.Base;
using Sddp.Abstractions.ValueObjects;

namespace Sddp.Domain.Entities;

/// <summary>
/// Outbox message entity (Transactional Outbox Pattern)
/// </summary>
public class OutboxMessage : EntityBase
{
    /// <summary>
    /// (: "UserCreated", "SpecApproved")
    /// </summary>
    public string EventType { get; private set; } = string.Empty;

    /// <summary>
    /// aggregated (: "User", "Spec")
    /// </summary>
    public string AggregateType { get; private set; } = string.Empty;

    /// <summary>
    /// aggregated ID
    /// </summary>
    public GlobalUniqueId AggregateId { get; private set; }

    /// <summary>
    /// (JSON)
    /// </summary>
    public string Payload { get; private set; } = string.Empty;

    /// <summary>
    /// (null)
    /// </summary>
    public Timestamp? ProcessedAt { get; private set; }

    /// <summary>
    ///
    /// </summary>
    public int RetryCount { get; private set; }

    /// <summary>
    ///
    /// </summary>
    public const int MaxRetryCount = 5;

    /// <summary>
    /// message
    /// </summary>
    public string? LastError { get; private set; }

    private OutboxMessage() { }

    public static OutboxMessage Create(
        string eventType,
        string aggregateType,
        GlobalUniqueId aggregateId,
        string payload)
    {
        if (string.IsNullOrWhiteSpace(eventType))
            throw new ArgumentException("Event type cannot be empty", nameof(eventType));

        if (string.IsNullOrWhiteSpace(aggregateType))
            throw new ArgumentException("Aggregate type cannot be empty", nameof(aggregateType));

        if (string.IsNullOrWhiteSpace(payload))
            throw new ArgumentException("Payload cannot be empty", nameof(payload));

        return new OutboxMessage
        {
            EventType = eventType,
            AggregateType = aggregateType,
            AggregateId = aggregateId,
            Payload = payload,
            RetryCount = 0
        };
    }

    /// <summary>
    ///
    /// </summary>
    public void MarkAsProcessed()
    {
        ProcessedAt = Timestamp.Now;
        MarkAsModified();
    }

    /// <summary>
    ///
    /// </summary>
    public void RecordFailure(string errorMessage)
    {
        RetryCount++;
        LastError = errorMessage;
        MarkAsModified();
    }

    /// <summary>
    ///
    /// </summary>
    public bool CanRetry => RetryCount < MaxRetryCount && ProcessedAt is null;

    /// <summary>
    ///
    /// </summary>
    public bool IsProcessed => ProcessedAt is not null;
}
