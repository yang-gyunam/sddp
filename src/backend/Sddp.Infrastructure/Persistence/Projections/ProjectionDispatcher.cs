using System.Collections.Concurrent;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Sddp.Domain.Events;
using Sddp.Infrastructure.Serialization;

namespace Sddp.Infrastructure.Persistence.Projections;

/// <summary>
/// Projection
/// </summary>
public class ProjectionDispatcher : IProjectionDispatcher
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ProjectionDispatcher> _logger;
    private static readonly ConcurrentDictionary<string, Type> EventTypeCache = new();

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        Converters =
        {
            new GlobalUniqueIdJsonConverter(),
            new TimestampJsonConverter()
        }
    };

    public ProjectionDispatcher(
        IServiceProvider serviceProvider,
        ILogger<ProjectionDispatcher> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public async Task DispatchAsync(string eventType, string payload, CancellationToken cancellationToken = default)
    {
        var type = ResolveEventType(eventType);
        if (type is null)
        {
            _logger.LogWarning("Unknown event type: {EventType}", eventType);
            return;
        }

        var @event = JsonSerializer.Deserialize(payload, type, JsonOptions);
        if (@event is null)
        {
            _logger.LogWarning("Failed to deserialize event: {EventType}", eventType);
            return;
        }

        await (DispatchToHandlersAsync(@event, type, cancellationToken)).ConfigureAwait(false);
    }

    private static Type? ResolveEventType(string eventTypeName)
    {
        if (EventTypeCache.TryGetValue(eventTypeName, out var cachedType))
        {
            return cachedType;
        }

        // domain search
        var domainAssembly = typeof(IDomainEvent).Assembly;
        var type = domainAssembly.GetTypes()
            .FirstOrDefault(t =>
                typeof(IDomainEvent).IsAssignableFrom(t) &&
                t.Name == eventTypeName);

        if (type is not null)
        {
            EventTypeCache.TryAdd(eventTypeName, type);
        }

        return type;
    }

    private async Task DispatchToHandlersAsync(object @event, Type eventType, CancellationToken cancellationToken)
    {
        var handlerType = typeof(IProjectionHandler<>).MakeGenericType(eventType);
        var handlers = _serviceProvider.GetServices(handlerType);

        foreach (var handler in handlers)
        {
            if (handler is null) continue;

            try
            {
                var method = handlerType.GetMethod(nameof(IProjectionHandler<IDomainEvent>.HandleAsync));
                if (method is not null)
                {
                    var task = method.Invoke(handler, [@event, cancellationToken]) as Task;
                    if (task is not null)
                    {
                        await (task).ConfigureAwait(false);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error dispatching event {EventType} to handler {HandlerType}",
                    eventType.Name, handler.GetType().Name);
            }
        }
    }
}
