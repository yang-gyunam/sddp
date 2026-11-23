using System.Diagnostics;
using System.Net.Sockets;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Sddp.Abstractions.DTOs;
using Sddp.Abstractions.Interfaces;

namespace Sddp.Api.HealthChecks;

public sealed record ServiceProbe(string Id, string Name, string Group, string Check,
    string? Endpoint, bool Required = false, bool Enabled = true, bool Tcp = false, string? Path = null);

/// <summary>One bounded snapshot per API process. Dependency status never changes the public readiness endpoint.</summary>
public sealed class ServiceHealthReader(HealthCheckService readiness, IHttpClientFactory clients,
    IEnumerable<ServiceProbe> probes) : IServiceHealthReader, IDisposable
{
    private readonly SemaphoreSlim _refresh = new(1);
    private HealthCheckDto? _last;

    public async Task<HealthCheckDto> ReadAsync(CancellationToken ct)
    {
        await _refresh.WaitAsync(ct);
        try
        {
            if (_last is not null && DateTimeOffset.UtcNow - _last.CheckedAt < TimeSpan.FromSeconds(10))
                return _last;

            var pending = new List<Task<ServiceHealthDto>>
            {
                Task.FromResult(new ServiceHealthDto("API Server", "Healthy", null, null, "api", "Application", "Authenticated API request")),
                ReadReadinessAsync("database", "PostgreSQL", "Connection, schema and base data", "database_readiness", ct),
                ReadReadinessAsync("redis", "Redis Cache", "TCP connection", "redis_connectivity", ct)
            };
            pending.AddRange(probes.Select(probe => CheckEndpointAsync(probe, ct)));
            var checks = await Task.WhenAll(pending);
            var overall = checks.Any(row => row.Required && row.Status == "Unhealthy") ? "Unhealthy"
                : checks.Any(row => row.Status is "Unhealthy" or "Warning" || (!row.Required && row.Status == "Unknown")) ? "Warning"
                : checks.Any(row => row.Required && row.Status != "Healthy") ? "Unknown" : "Healthy";
            ct.ThrowIfCancellationRequested();
            return _last = new(overall, checks, DateTimeOffset.UtcNow);
        }
        finally { _refresh.Release(); }
    }

    private async Task<ServiceHealthDto> ReadReadinessAsync(string id, string name, string check, string registration, CancellationToken ct)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
        timeout.CancelAfter(TimeSpan.FromSeconds(4));
        try
        {
            var report = await readiness.CheckHealthAsync(r => r.Name == registration, timeout.Token);
            if (!report.Entries.TryGetValue(registration, out var entry))
                return new(name, "Unknown", "Check is not registered", null, id, "Data", check);

            var status = entry.Status switch
            {
                HealthStatus.Healthy => "Healthy",
                HealthStatus.Degraded => "Warning",
                _ => "Unhealthy"
            };
            return new(name, status, entry.Status == HealthStatus.Healthy ? null : "Readiness check failed",
                (long)entry.Duration.TotalMilliseconds, id, "Data", check);
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested)
        {
            return new(name, "Unknown", "Readiness check timed out", null, id, "Data", check);
        }
    }

    private async Task<ServiceHealthDto> CheckEndpointAsync(ServiceProbe probe, CancellationToken ct)
    {
        ServiceHealthDto Result(string status, string? message = null, long? elapsed = null) =>
            new(probe.Name, status, message, elapsed, probe.Id, probe.Group, probe.Check, probe.Required);
        if (!probe.Enabled) return Result("Disabled");
        if (string.IsNullOrWhiteSpace(probe.Endpoint)) return Result("NotConfigured");
        if (!Uri.TryCreate(probe.Endpoint, UriKind.Absolute, out var endpoint) ||
            endpoint.Scheme is not ("http" or "https") || endpoint.UserInfo.Length != 0)
            return Result("Unknown", "Invalid endpoint configuration");

        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
        timeout.CancelAfter(TimeSpan.FromSeconds(3));
        var watch = Stopwatch.StartNew();
        try
        {
            if (probe.Tcp)
            {
                using var socket = new TcpClient();
                await socket.ConnectAsync(endpoint.Host, endpoint.Port, timeout.Token);
            }
            else
            {
                var url = probe.Path is null ? endpoint : new Uri(endpoint, probe.Path);
                using var client = clients.CreateClient("ServiceHealth");
                using var response = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, timeout.Token);
                if (!response.IsSuccessStatusCode || (probe.Id == "web" && (int)response.StatusCode != 204))
                    return Result("Unhealthy", $"Unexpected HTTP {(int)response.StatusCode}", watch.ElapsedMilliseconds);
            }
            return Result("Healthy", elapsed: watch.ElapsedMilliseconds);
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested)
        { return Result("Unhealthy", "Check timed out", watch.ElapsedMilliseconds); }
        catch (Exception e) when (e is HttpRequestException or SocketException or IOException)
        { return Result("Unhealthy", "Connection failed", watch.ElapsedMilliseconds); }
    }

    public void Dispose() => _refresh.Dispose();
}
