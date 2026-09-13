using System.Text.Json;
using Microsoft.Extensions.Logging;
using Sddp.Abstractions.Events;
using Sddp.Abstractions.Interfaces;
using Sddp.Domain.Entities;

namespace Sddp.Infrastructure.Persistence.Outbox;

public sealed class TimelineOutboxMessageHandler : IOutboxMessageHandler
{
    /// <summary>The event type this handler answers to. Anything else is not its business.</summary>
    public const string EventType = nameof(TimelineActivityCreatedEvent);

    private static readonly JsonSerializerOptions JsonOptions =
        new(JsonSerializerDefaults.Web);

    private readonly ITimelineNotificationService _notification;
    private readonly ILogger<TimelineOutboxMessageHandler> _logger;

    public TimelineOutboxMessageHandler(
        ITimelineNotificationService notification,
        ILogger<TimelineOutboxMessageHandler> logger)
    {
        _notification = notification;
        _logger = logger;
    }

    public async Task HandleAsync(OutboxMessage message, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);

        if (!string.Equals(message.EventType, EventType, StringComparison.Ordinal))
        {
            return;
        }

        var activity = JsonSerializer.Deserialize<TimelineActivityCreatedEvent>(
            message.Payload, JsonOptions);

        if (activity is null)
        {
            // Dropping it rather than throwing: a payload that cannot be read will never become
            // readable, and retrying it forever would block nothing but itself while filling the
            // log. The message stays unprocessed and ages out of the retry window.
            _logger.LogWarning(
                "Outbox message {MessageId} carries an unreadable timeline payload", message.Id);
            return;
        }

        await _notification.NotifyActivityCreatedAsync(
            activity.TenantId,
            activity.ProjectId,
            activity.Activity,
            cancellationToken).ConfigureAwait(false);
    }
}

/// <summary>
/// Runs every registered handler for one message.
///
/// There was exactly one handler, so adding a second would have silently replaced the first in
/// DI. Each handler decides for itself whether a message is its business.
/// </summary>
public sealed class CompositeOutboxMessageHandler : IOutboxMessageHandler
{
    private readonly IReadOnlyList<IOutboxMessageHandler> _handlers;

    public CompositeOutboxMessageHandler(IEnumerable<IOutboxMessageHandler> handlers)
    {
        _handlers = handlers.Where(handler => handler is not CompositeOutboxMessageHandler).ToList();
    }

    public async Task HandleAsync(OutboxMessage message, CancellationToken cancellationToken = default)
    {
        foreach (var handler in _handlers)
        {
            await handler.HandleAsync(message, cancellationToken).ConfigureAwait(false);
        }
    }
}
