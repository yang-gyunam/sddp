using System.Diagnostics;
using OpenTelemetry;
using Sddp.Diagnostics;

namespace Sddp.Api.Extensions;

/// <summary>Keep operation names, SQL structure, status, stack and error reasons visible to the operator.</summary>
internal sealed class OperationalTraceProcessor : BaseProcessor<Activity>
{
    public override void OnEnd(Activity activity)
    {
        var sql = activity.Source.Name is "Microsoft.EntityFrameworkCore" or "OpenTelemetry.Instrumentation.EntityFrameworkCore";
        activity.DisplayName = sql
            ? DiagnosticMask.Sql(activity.DisplayName)!
            : DiagnosticMask.Text(activity.DisplayName)!;
        if (activity.StatusDescription is { } reason)
            activity.SetStatus(activity.Status, DiagnosticMask.Text(reason));
        foreach (var tag in activity.TagObjects.ToArray())
        {
            var masked = Mask(tag.Key, tag.Value);
            if (!Equals(masked, tag.Value)) activity.SetTag(tag.Key, masked);
        }
        foreach (var evt in activity.Events)
        {
            if (evt.Tags is not ActivityTagsCollection tags) continue;
            foreach (var tag in tags.ToArray()) tags[tag.Key] = Mask(tag.Key, tag.Value);
        }
    }

    private static object? Mask(string key, object? value)
    {
        if (key.StartsWith("db.query.parameter.", StringComparison.Ordinal)
            || key.StartsWith("http.request.header.", StringComparison.OrdinalIgnoreCase)
            || key.StartsWith("http.response.header.", StringComparison.OrdinalIgnoreCase))
            return DiagnosticMask.Hidden;
        // Numeric token counts remain useful and contain no credential.
        if (key is "gen_ai.usage.input_tokens" or "gen_ai.usage.output_tokens" or "gen_ai.request.max_tokens"
            or "input.tokens" or "output.tokens" or "input.prompt.tokens") return value;
        if (DiagnosticMask.IsSensitiveKey(key) || key is "url.query" or "url.fragment") return DiagnosticMask.Hidden;
        return value is not string text ? value : key switch
        {
            "db.statement" or "db.query.text" => DiagnosticMask.Sql(text),
            "url.full" or "url.path" or "http.target" or "http.url" => DiagnosticMask.Url(text),
            _ => DiagnosticMask.Text(text, 16000)
        };
    }
}
