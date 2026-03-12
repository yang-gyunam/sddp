using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Sddp.Infrastructure.Persistence.Outbox;

/// <summary>
/// Outbox message
/// </summary>
public class OutboxBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<OutboxBackgroundService> _logger;
    private readonly TimeSpan _pollingInterval = TimeSpan.FromSeconds(5);
    private readonly TimeSpan _retryInterval = TimeSpan.FromMinutes(1);

    public OutboxBackgroundService(
        IServiceScopeFactory scopeFactory,
        ILogger<OutboxBackgroundService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Outbox background service started");

        var lastRetryTime = DateTime.UtcNow;

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var processor = scope.ServiceProvider.GetRequiredService<IOutboxProcessor>();

                // new message
                await (processor.ProcessPendingMessagesAsync(stoppingToken)).ConfigureAwait(false);

                // message
                if (DateTime.UtcNow - lastRetryTime >= _retryInterval)
                {
                    await (processor.RetryFailedMessagesAsync(stoppingToken)).ConfigureAwait(false);
                    lastRetryTime = DateTime.UtcNow;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing outbox messages");
            }

            await (Task.Delay(_pollingInterval, stoppingToken)).ConfigureAwait(false);
        }

        _logger.LogInformation("Outbox background service stopped");
    }
}
