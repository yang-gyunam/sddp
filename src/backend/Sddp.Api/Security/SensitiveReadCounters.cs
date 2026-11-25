using StackExchange.Redis;

namespace Sddp.Api.Security;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
internal sealed class SensitiveReadAttribute(string surface) : Attribute
{
    internal string Surface { get; } = surface;
}

internal readonly record struct SensitiveRead(Guid TenantId, Guid UserId, Guid? ProjectId,
    string Surface, long Bytes, DateTimeOffset ObservedAt);

internal readonly record struct ReadThreshold(Guid? ProjectId, DateTimeOffset WindowStart,
    ReadWindow Window, long Bytes, long Requests);

internal static class SensitiveReadCounters
{
    private const string Update = """
        local alerts = {}
        local now = tonumber(redis.call('TIME')[1])
        for i, key in ipairs(KEYS) do
            local p = 2 + (i - 1) * 3
            if tonumber(ARGV[p + 2]) > now then
                local bytes = redis.call('HINCRBY', key, 'bytes', ARGV[1])
                local requests = redis.call('HINCRBY', key, 'requests', 1)
                if (bytes >= tonumber(ARGV[p]) or requests >= tonumber(ARGV[p + 1]))
                    and redis.call('HSETNX', key, 'alerted', 1) == 1 then
                    table.insert(alerts, i - 1)
                    table.insert(alerts, bytes)
                    table.insert(alerts, requests)
                end
                if requests == 1 then redis.call('EXPIREAT', key, ARGV[p + 2]) end
            end
        end
        return alerts
        """;

    internal static async Task<IReadOnlyList<ReadThreshold>> RecordAsync(IDatabase store, string prefix,
        SensitiveRead read, IReadOnlyList<ReadWindow> windows)
    {
        var keys = new List<RedisKey>();
        var arguments = new List<RedisValue> { read.Bytes };
        var scopes = new List<(Guid? Project, long Start, ReadWindow Window)>();
        var second = read.ObservedAt.ToUnixTimeSeconds();

        // The hash tag keeps all windows for this user in one Redis Cluster slot.
        var user = $"{prefix}security:sensitive-reads:{{{read.TenantId:N}:{read.UserId:N}}}";
        AddScope(null);
        if (read.ProjectId is { } project) AddScope(project);

        var result = (RedisResult[])(await store.ScriptEvaluateAsync(Update, keys.ToArray(), arguments.ToArray()))!;
        var alerts = new List<ReadThreshold>(result.Length / 3);
        for (var i = 0; i < result.Length; i += 3)
        {
            var scope = scopes[(int)result[i]];
            alerts.Add(new(scope.Project, DateTimeOffset.FromUnixTimeSeconds(scope.Start), scope.Window,
                (long)result[i + 1], (long)result[i + 2]));
        }
        return alerts;

        void AddScope(Guid? projectId)
        {
            var scope = projectId?.ToString("N") ?? "all";
            foreach (var window in windows)
            {
                var start = second - second % window.Seconds;
                keys.Add($"{user}:{scope}:{window.Seconds}:{start}");
                arguments.Add(window.ByteLimit);
                arguments.Add(window.RequestLimit);
                // Keep the completed bucket briefly for observations already queued on another host.
                arguments.Add(start + window.Seconds + 60);
                scopes.Add((projectId, start, window));
            }
        }
    }
}
