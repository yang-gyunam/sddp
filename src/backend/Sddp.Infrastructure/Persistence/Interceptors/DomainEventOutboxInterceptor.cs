using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;
using Sddp.Abstractions.Base;
using Sddp.Domain.Entities;
using Sddp.Domain.Events;
using Sddp.Infrastructure.Serialization;

namespace Sddp.Infrastructure.Persistence.Interceptors;

/// <summary>
/// domain Outbox Interceptor
/// SaveChanges entity domain OutboxMessage
/// </summary>
public class DomainEventOutboxInterceptor : SaveChangesInterceptor
{
    private readonly ILogger<DomainEventOutboxInterceptor> _logger;

    public DomainEventOutboxInterceptor(ILogger<DomainEventOutboxInterceptor> logger)
    {
        _logger = logger;
    }

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = false,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters =
        {
            new GlobalUniqueIdJsonConverter(),
            new TimestampJsonConverter()
        }
    };

    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result)
    {
        AddDomainEventsToOutbox(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        AddDomainEventsToOutbox(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void AddDomainEventsToOutbox(DbContext? context)
    {
        if (context is null)
        {
            _logger.LogWarning("DomainEventOutboxInterceptor: DbContext is null, domain events will not be persisted to outbox");
            return;
        }

        // domain entity
        var entitiesWithEvents = context.ChangeTracker
            .Entries<EntityBase>()
            .Where(e => e.Entity.DomainEvents.Any())
            .Select(e => e.Entity)
            .ToList();

        foreach (var entity in entitiesWithEvents)
        {
            foreach (var domainEvent in entity.DomainEvents)
            {
                var outboxMessage = CreateOutboxMessage(entity, domainEvent);
                context.Set<OutboxMessage>().Add(outboxMessage);
            }

            //
            entity.ClearDomainEvents();
        }
    }

    private static OutboxMessage CreateOutboxMessage(EntityBase entity, object domainEvent)
    {
        string eventType;
        string aggregateType;

        if (domainEvent is AggregateEvent aggregateEvent)
        {
            eventType = aggregateEvent.EventType;
            aggregateType = aggregateEvent.AggregateType;
        }
        else if (domainEvent is IDomainEvent baseEvent)
        {
            eventType = baseEvent.EventType;
            aggregateType = entity.GetType().Name;
        }
        else
        {
            eventType = domainEvent.GetType().Name;
            aggregateType = entity.GetType().Name;
        }

        var payload = JsonSerializer.Serialize(domainEvent, domainEvent.GetType(), JsonOptions);

        return OutboxMessage.Create(
            eventType: eventType,
            aggregateType: aggregateType,
            aggregateId: entity.Id,
            payload: payload);
    }
}
