using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Sddp.Domain.Entities;
using Sddp.Infrastructure.Persistence.Contexts;
using Sddp.Infrastructure.Persistence.Projections;

namespace Sddp.Infrastructure.Persistence.Outbox;

/// <summary>
/// Outbox message
/// </summary>
public class OutboxProcessor : IOutboxProcessor
{
    private readonly SddpDbContext _context;
    private readonly ILogger<OutboxProcessor> _logger;
    private readonly IOutboxMessageHandler _messageHandler;

    public OutboxProcessor(
        SddpDbContext context,
        ILogger<OutboxProcessor> logger,
        IOutboxMessageHandler messageHandler)
    {
        _context = context;
        _logger = logger;
        _messageHandler = messageHandler;
    }

    public async Task ProcessPendingMessagesAsync(CancellationToken cancellationToken = default)
    {
        var messages = await (_context.OutboxMessages
            .Where(m => m.ProcessedAt == null && m.IsActive)
            .OrderBy(m => m.CreatedAt)
.Take(100) // layout 
            .ToListAsync(cancellationToken)).ConfigureAwait(false);

        foreach (var message in messages)
        {
            await (ProcessMessageAsync(message, cancellationToken)).ConfigureAwait(false);
        }
    }

    public async Task RetryFailedMessagesAsync(CancellationToken cancellationToken = default)
    {
        var messages = await (_context.OutboxMessages
            .Where(m => m.ProcessedAt == null
                && m.IsActive
                && m.RetryCount > 0
                && m.RetryCount < OutboxMessage.MaxRetryCount)
            .OrderBy(m => m.CreatedAt)
            .Take(50)
            .ToListAsync(cancellationToken)).ConfigureAwait(false);

        foreach (var message in messages)
        {
            await (ProcessMessageAsync(message, cancellationToken)).ConfigureAwait(false);
        }
    }

    private async Task ProcessMessageAsync(OutboxMessage message, CancellationToken cancellationToken)
    {
        try
        {
            await (_messageHandler.HandleAsync(message, cancellationToken)).ConfigureAwait(false);
            message.MarkAsProcessed();
            _logger.LogInformation(
                "Outbox message {MessageId} processed successfully. EventType: {EventType}",
                message.Id, message.EventType);
        }
        catch (Exception ex)
        {
            message.RecordFailure(ex.Message);
            _logger.LogWarning(
                ex,
                "Failed to process outbox message {MessageId}. Retry count: {RetryCount}",
                message.Id, message.RetryCount);
        }

        await (_context.SaveChangesAsync(cancellationToken)).ConfigureAwait(false);
    }
}

/// <summary>
/// Outbox message
/// </summary>
public interface IOutboxMessageHandler
{
    Task HandleAsync(OutboxMessage message, CancellationToken cancellationToken = default);
}

/// <summary>
/// Outbox message (Projection Dispatcher)
/// </summary>
public class ProjectionOutboxMessageHandler : IOutboxMessageHandler
{
    private readonly IProjectionDispatcher _projectionDispatcher;
    private readonly ILogger<ProjectionOutboxMessageHandler> _logger;

    public ProjectionOutboxMessageHandler(
        IProjectionDispatcher projectionDispatcher,
        ILogger<ProjectionOutboxMessageHandler> logger)
    {
        _projectionDispatcher = projectionDispatcher;
        _logger = logger;
    }

    public async Task HandleAsync(OutboxMessage message, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug(
            "Dispatching outbox message to projections. EventType: {EventType}, AggregateType: {AggregateType}, AggregateId: {AggregateId}",
            message.EventType, message.AggregateType, message.AggregateId);

        await (_projectionDispatcher.DispatchAsync(message.EventType, message.Payload, cancellationToken)).ConfigureAwait(false);
    }
}
