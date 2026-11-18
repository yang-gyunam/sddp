using System.Globalization;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Dapper;
using Npgsql;

namespace Sddp.Api.Services;

public sealed record StatisticCell(string Action, long Events, long Actors);
public sealed record WeeklyCounts(int Version, string Week, Dictionary<string, long> Counts);
public sealed record StatisticsView(bool Enabled, string? Recipient, int? RetentionDays, string Week,
    IReadOnlyList<StatisticCell> InternalTotals, WeeklyCounts TransferPreview);

public sealed class WeeklyStatistics(IConfiguration config, IHttpClientFactory factory)
{
    private NpgsqlConnection Database() => new(config.GetConnectionString("DefaultConnection"));
    internal const string Client = "weekly-statistics";
    private sealed record Pending(Guid Seed, DateTime Week, string Payload);
    private sealed record Recipient(Uri Url, string Token, int Days, string Hash);

    private Recipient? Destination()
    {
        if (!Uri.TryCreate(config["ProductStatistics:Recipient"], UriKind.Absolute, out var uri)
            || uri.Scheme != Uri.UriSchemeHttps || uri.UserInfo.Length != 0 || uri.Query.Length != 0 || uri.Fragment.Length != 0
            || config["ProductStatistics:Token"] is not { Length: >= 32 } token
            || !int.TryParse(config["ProductStatistics:RecipientRetentionDays"], out var days) || days is < 1 or > 3650)
            return null;
        return new(uri, token, days, Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes($"usage-v1|{uri.AbsoluteUri}|{days}"))));
    }

    internal static DateTime LastWeek(DateTime utc)
        => utc.Date.AddDays(-((int)utc.DayOfWeek + 6) % 7 - 7);

    internal static WeeklyCounts Project(DateTime week, IEnumerable<StatisticCell> cells)
        => new(1, week.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            cells.Where(c => c.Actors >= 5 && c.Events >= 5 && c.Action is "create" or "update" or "delete")
                .ToDictionary(c => c.Action, c => Math.Min(1_000_000, c.Events / 5 * 5), StringComparer.Ordinal));

    private static async Task<StatisticCell[]> Counts(NpgsqlConnection db, Guid tenant, DateTime week, CancellationToken ct)
    {
        var rows = await db.QueryAsync<StatisticCell>(new CommandDefinition("""
            SELECT lower(action) AS Action,count(*) AS Events,count(distinct actor_id) AS Actors
            FROM audit_logs WHERE tenant_id=@tenant AND actor_id IS NOT NULL AND actor_type='USER'
              AND created_at>=@week AND created_at<@end AND upper(action) IN ('CREATE','UPDATE','DELETE')
            GROUP BY lower(action) ORDER BY lower(action)
            """, new { tenant, week, end = week.AddDays(7) }, cancellationToken: ct));
        return rows.ToArray();
    }

    public async Task<StatisticsView> Read(Guid tenant, CancellationToken ct)
    {
        await using var db = Database();
        var dest = Destination();
        var week = LastWeek(DateTime.UtcNow);
        var counts = await Counts(db, tenant, week, ct);
        var saved = await db.QuerySingleOrDefaultAsync<string>(new CommandDefinition(
            "SELECT recipient_hash FROM statistics_consents WHERE tenant_id=@tenant", new { tenant }, cancellationToken: ct));
        return new(dest is not null && saved == dest.Hash, dest?.Url.AbsoluteUri, dest?.Days, week.ToString("yyyy-MM-dd"),
            counts, Project(week, counts));
    }

    public async Task<bool> Set(Guid tenant, Guid user, bool enabled, string? seenRecipient, int? seenDays, CancellationToken ct)
    {
        await using var db = Database();
        if (!enabled)
        {
            await db.ExecuteAsync(new CommandDefinition("DELETE FROM statistics_consents WHERE tenant_id=@tenant",
                new { tenant }, cancellationToken: ct));
            return true;
        }
        var dest = Destination();
        if (dest is null || dest.Url.AbsoluteUri != seenRecipient || dest.Days != seenDays) return false;
        await db.ExecuteAsync(new CommandDefinition("""
            INSERT INTO statistics_consents(tenant_id,delivery_seed,recipient_hash,enabled_at,enabled_by)
            VALUES(@tenant,@seed,@hash,now(),@user) ON CONFLICT(tenant_id) DO UPDATE SET
            delivery_seed=excluded.delivery_seed,recipient_hash=excluded.recipient_hash,enabled_at=now(),
            enabled_by=excluded.enabled_by,last_delivered_week=NULL,pending_week=NULL,pending_counts=NULL
            WHERE statistics_consents.recipient_hash<>excluded.recipient_hash
            """, new { tenant, seed = Guid.NewGuid(), hash = dest.Hash, user }, cancellationToken: ct));
        return true;
    }

    internal async Task Dispatch(DateTime utc, CancellationToken ct)
    {
        var dest = Destination();
        if (dest is null) return;
        var week = LastWeek(utc);
        await using var db = Database();
        await db.OpenAsync(ct);
        var eligible = (await db.QueryAsync<Guid>(new CommandDefinition("""
            SELECT tenant_id FROM statistics_consents WHERE recipient_hash=@hash AND enabled_at<=@week
              AND (last_delivered_week IS NULL OR last_delivered_week<@week) ORDER BY tenant_id
            """, new { hash = dest.Hash, week }, cancellationToken: ct))).ToArray();
        foreach (var tenant in eligible)
        {
            // Freeze the report before network I/O so an ambiguous acknowledgement can be retried byte-for-byte.
            await using (var prepare = await db.BeginTransactionAsync(ct))
            {
                var ready = await db.QuerySingleOrDefaultAsync<Guid?>(new CommandDefinition("""
                    SELECT tenant_id FROM statistics_consents WHERE tenant_id=@tenant AND recipient_hash=@hash
                      AND enabled_at<=@week AND (last_delivered_week IS NULL OR last_delivered_week<@week)
                      AND pending_week IS NULL FOR UPDATE SKIP LOCKED
                    """, new { tenant, hash = dest.Hash, week }, prepare, cancellationToken: ct));
                if (ready.HasValue)
                {
                    var payload = Project(week, await Counts(db, tenant, week, ct));
                    if (payload.Counts.Count == 0)
                        await db.ExecuteAsync(new CommandDefinition("UPDATE statistics_consents SET last_delivered_week=@week WHERE tenant_id=@tenant",
                            new { tenant, week }, prepare, cancellationToken: ct));
                    else
                        await db.ExecuteAsync(new CommandDefinition("""
                            UPDATE statistics_consents SET pending_week=@week,pending_counts=CAST(@json AS jsonb) WHERE tenant_id=@tenant
                            """, new { tenant, week, json = JsonSerializer.Serialize(payload, JsonSerializerOptions.Web) }, prepare, cancellationToken: ct));
                }
                await prepare.CommitAsync(ct);
            }
            // Withdrawal and delivery take the same row lock. Changing recipient invalidates pending consent.
            await using var tx = await db.BeginTransactionAsync(ct);
            var pending = await db.QuerySingleOrDefaultAsync<Pending>(new CommandDefinition("""
                SELECT delivery_seed AS Seed,pending_week::timestamp AS Week,pending_counts::text AS Payload
                FROM statistics_consents WHERE tenant_id=@tenant AND recipient_hash=@hash AND pending_week IS NOT NULL
                FOR UPDATE SKIP LOCKED
                """, new { tenant, hash = dest.Hash }, tx, cancellationToken: ct));
            if (pending is null) continue;
            {
                var delivery = Convert.ToHexStringLower(HMACSHA256.HashData(pending.Seed.ToByteArray(),
                    Encoding.ASCII.GetBytes(pending.Week.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture))));
                using var request = new HttpRequestMessage(HttpMethod.Post, dest.Url) { Content = new StringContent(pending.Payload, Encoding.UTF8, "application/json") };
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", dest.Token);
                request.Headers.Add("Idempotency-Key", delivery);
                using var response = await factory.CreateClient(Client).SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct);
                if (!response.IsSuccessStatusCode) continue;
            }
            await db.ExecuteAsync(new CommandDefinition("UPDATE statistics_consents SET last_delivered_week=pending_week,pending_week=NULL,pending_counts=NULL WHERE tenant_id=@tenant",
                new { tenant }, tx, cancellationToken: ct));
            await tx.CommitAsync(ct);
        }
    }
}

internal sealed class WeeklyStatisticsWorker(WeeklyStatistics statistics, ILogger<WeeklyStatisticsWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromHours(1));
        do
        {
            try { await statistics.Dispatch(DateTime.UtcNow, stoppingToken); }
            catch (Exception) when (!stoppingToken.IsCancellationRequested) { logger.LogWarning("Weekly statistics delivery deferred"); }
        } while (await timer.WaitForNextTickAsync(stoppingToken));
    }
}
