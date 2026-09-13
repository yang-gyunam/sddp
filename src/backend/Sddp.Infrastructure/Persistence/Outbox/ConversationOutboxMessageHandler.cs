using System.Text.Json;
using Microsoft.Extensions.Logging;
using Sddp.Abstractions.Events;
using Sddp.Abstractions.Interfaces;
using Sddp.Domain.Entities;

namespace Sddp.Infrastructure.Persistence.Outbox;

public sealed class ConversationOutboxMessageHandler : IOutboxMessageHandler
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly IConversationHubService _hub;
    private readonly ILogger<ConversationOutboxMessageHandler> _logger;

    public ConversationOutboxMessageHandler(
        IConversationHubService hub,
        ILogger<ConversationOutboxMessageHandler> logger)
    {
        _hub = hub;
        _logger = logger;
    }

    public async Task HandleAsync(OutboxMessage message, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);

        switch (message.EventType)
        {
            case nameof(ConversationMemberRemovedEvent):
                if (TryRead<ConversationMemberRemovedEvent>(message) is { } removed)
                {
                    await _hub.BroadcastMemberRemovedAsync(
                        removed.ConversationId, removed.RemovedUserId, removed.RemovedUserName)
                        .ConfigureAwait(false);
                }
                break;

            case nameof(ProjectChannelCreatedEvent):
                if (TryRead<ProjectChannelCreatedEvent>(message) is { } created)
                {
                    await _hub.NotifyProjectChannelCreatedAsync(
                        created.MemberUserIds, created.ConversationId, created.ProjectId,
                        created.ChannelName, created.CreatedByUserId)
                        .ConfigureAwait(false);
                }
                break;

            case nameof(ConversationClosedEvent):
                if (TryRead<ConversationClosedEvent>(message) is { } closed)
                {
                    await _hub.BroadcastConversationClosedAsync(
                        closed.ConversationId, closed.DecisionSpecId, closed.ConcludedByUserName)
                        .ConfigureAwait(false);
                }
                break;

            case nameof(DirectMessageConcludedEvent):
                if (TryRead<DirectMessageConcludedEvent>(message) is { } concluded)
                {
                    await _hub.NotifyDMConcludedAsync(
                        concluded.MemberUserIds, concluded.ConversationId,
                        concluded.ConcludedByUserName)
                        .ConfigureAwait(false);
                }
                break;

            // Anything else belongs to another handler. The composite runs them all.
            default:
                break;
        }
    }

    /// <summary>
    /// Reads a payload, or null when it cannot be read.
    ///
    /// Dropping rather than throwing: a payload that cannot be deserialised will not become
    /// readable on a retry, and throwing would leave it cycling until it exhausts its retries
    /// while filling the log. Same rule as <see cref="TimelineOutboxMessageHandler"/>.
    /// </summary>
    private T? TryRead<T>(OutboxMessage message) where T : class
    {
        try
        {
            var payload = JsonSerializer.Deserialize<T>(message.Payload, JsonOptions);
            if (payload is null)
            {
                _logger.LogWarning(
                    "Outbox message {MessageId} carries an empty {EventType} payload",
                    message.Id, message.EventType);
            }

            return payload;
        }
        catch (JsonException error)
        {
            _logger.LogWarning(
                error,
                "Outbox message {MessageId} carries an unreadable {EventType} payload",
                message.Id, message.EventType);
            return null;
        }
    }
}
