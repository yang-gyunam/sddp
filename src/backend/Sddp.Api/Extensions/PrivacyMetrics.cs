using System.Diagnostics.Metrics;
using System.Text.Json;

namespace Sddp.Api.Extensions;

internal sealed class PrivacyMetrics : IDisposable
{
    internal const string Name = "Sddp.Privacy";
    private static readonly string[] Fields = ["openHolds", "requestsAwaitingReview", "pendingDeletionBatches", "reviewedDeletions"];
    private readonly Meter _meter = new(Name);

    internal PrivacyMetrics(string path)
    {
        _meter.CreateObservableGauge("sddp.privacy.records", () => Read(path));
    }

    internal static Measurement<long>[] Read(string path)
    {
        try
        {
            using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read | FileShare.Delete);
            if (stream.Length > 4096) return [];
            using var doc = JsonDocument.Parse(stream);
            if (doc.RootElement.ValueKind != JsonValueKind.Object) return [];
            var counts = new List<Measurement<long>>();
            foreach (var field in Fields)
            {
                if (!doc.RootElement.TryGetProperty(field, out var cell) || cell.ValueKind != JsonValueKind.Number
                    || !cell.TryGetInt64(out var value) || value < 0) return [];
                counts.Add(new(value, new KeyValuePair<string, object?>("state", field)));
            }
            return counts.ToArray();
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException) { return []; }
    }

    public void Dispose() => _meter.Dispose();
}
