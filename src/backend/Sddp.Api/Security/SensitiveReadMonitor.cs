using System.Threading.Channels;
using Microsoft.Extensions.Options;
using Sddp.Api.Services;
using StackExchange.Redis;

namespace Sddp.Api.Security;

internal sealed class SensitiveReadMonitor(IServiceProvider services, IOptions<SensitiveReadOptions> options,
    ILogger<SensitiveReadMonitor> logger) : BackgroundService
{
    private readonly Channel<SensitiveRead> _reads = Channel.CreateBounded<SensitiveRead>(new BoundedChannelOptions(4096)
    {
        SingleReader = true
    });
    private long _nextDegradedLog;
    private volatile bool _running;

    internal bool IsEnabled => _running && options.Value.Enabled;

    internal void Observe(SensitiveRead read)
    {
        if (!_reads.Writer.TryWrite(read)) Degraded("queue-full");
    }

    public override Task StartAsync(CancellationToken cancellationToken)
    {
        _running = options.Value.Enabled;
        return base.StartAsync(cancellationToken);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!options.Value.Enabled) return;
        var windows = options.Value.Windows();
        await foreach (var read in _reads.Reader.ReadAllAsync(stoppingToken))
        {
            try
            {
                var redis = services.GetRequiredService<IConnectionMultiplexer>();
                if (!redis.IsConnected)
                {
                    Degraded("redis-disconnected");
                    continue;
                }

                var prefix = services.GetRequiredService<RedisCacheKeyPrefix>().Value;
                foreach (var alert in await SensitiveReadCounters.RecordAsync(redis.GetDatabase(), prefix, read, windows))
                    logger.LogWarning("SDDP_SENSITIVE_READ_THRESHOLD TenantId={TenantId} UserId={UserId} ProjectId={ProjectId} " +
                        "WindowStartUtc={WindowStartUtc} WindowSeconds={WindowSeconds} Bytes={Bytes} Requests={Requests} " +
                        "ByteLimit={ByteLimit} RequestLimit={RequestLimit} LastSurface={LastSurface}",
                        read.TenantId, read.UserId, alert.ProjectId, alert.WindowStart, alert.Window.Seconds,
                        alert.Bytes, alert.Requests, alert.Window.ByteLimit, alert.Window.RequestLimit, read.Surface);
            }
            catch (Exception error)
            {
                Degraded(error.GetType().Name);
            }
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _running = false;
        _reads.Writer.TryComplete();
        if (ExecuteTask is { } worker)
            await Task.WhenAny(worker, Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken));
        await base.StopAsync(cancellationToken);
    }

    private void Degraded(string reason)
    {
        var now = Environment.TickCount64;
        var before = Volatile.Read(ref _nextDegradedLog);
        if (now >= before && Interlocked.CompareExchange(ref _nextDegradedLog, now + 60_000, before) == before)
            logger.LogWarning("SDDP_SENSITIVE_READ_MONITOR_DEGRADED Reason={Reason}", reason);
    }
}
