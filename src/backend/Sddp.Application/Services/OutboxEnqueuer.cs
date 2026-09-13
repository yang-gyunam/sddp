using System.Text.Json;
using Sddp.Abstractions.Interfaces;
using Sddp.Abstractions.ValueObjects;
using Sddp.Domain.Entities;

namespace Sddp.Application.Services;

/// <inheritdoc cref="IOutboxEnqueuer"/>
public sealed class OutboxEnqueuer : IOutboxEnqueuer
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly IUnitOfWork _unitOfWork;

    public OutboxEnqueuer(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task EnqueueAsync<TEvent>(
        TEvent @event,
        string aggregateType,
        GlobalUniqueId aggregateId,
        CancellationToken cancellationToken = default)
        where TEvent : class
    {
        ArgumentNullException.ThrowIfNull(@event);

        await (_unitOfWork.Repository<OutboxMessage>().AddAsync(
            OutboxMessage.Create(
                typeof(TEvent).Name,
                aggregateType,
                aggregateId,
                JsonSerializer.Serialize(@event, JsonOptions)),
            cancellationToken)).ConfigureAwait(false);

        // The half that is easy to forget. CommitAsync will not save this row: callers have
        // already run SaveChanges inside the transaction, which makes CommitAsync skip its own
        // save. Without this line the row is tracked, never written, and the notification is lost
        // with no error anywhere -- the exact failure AuditBehavior hit first.
        await (_unitOfWork.SaveChangesAsync(cancellationToken)).ConfigureAwait(false);
    }
}
