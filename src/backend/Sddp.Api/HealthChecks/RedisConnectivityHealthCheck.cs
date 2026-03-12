using System.Net.Sockets;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Sddp.Api.HealthChecks;

/// <summary>
/// Verifies Redis TCP connectivity from the API container/runtime.
/// </summary>
public sealed class RedisConnectivityHealthCheck(IConfiguration configuration) : IHealthCheck
{
    private readonly string _redisConnectionString = configuration["Redis:ConnectionString"] ?? "localhost:6379";

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var (host, port) = ParseEndpoint(_redisConnectionString);

            using var client = new TcpClient();
            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeoutCts.CancelAfter(TimeSpan.FromSeconds(2));

            await client.ConnectAsync(host, port, timeoutCts.Token);
            return HealthCheckResult.Healthy($"Redis reachable at {host}:{port}.");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Redis is not reachable.", ex);
        }
    }

    private static (string Host, int Port) ParseEndpoint(string connectionString)
    {
        var firstSegment = connectionString.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .FirstOrDefault();

        if (string.IsNullOrWhiteSpace(firstSegment))
        {
            return ("localhost", 6379);
        }

        if (firstSegment.StartsWith("redis://", StringComparison.OrdinalIgnoreCase) ||
            firstSegment.StartsWith("rediss://", StringComparison.OrdinalIgnoreCase))
        {
            var uri = new Uri(firstSegment);
            return (uri.Host, uri.Port > 0 ? uri.Port : 6379);
        }

        var hostPort = firstSegment.Split(':', 2, StringSplitOptions.TrimEntries);
        var host = hostPort[0];
        var port = hostPort.Length > 1 && int.TryParse(hostPort[1], out var parsedPort) ? parsedPort : 6379;

        return (host, port);
    }
}
