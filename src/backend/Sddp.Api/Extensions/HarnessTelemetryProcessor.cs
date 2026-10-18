using System.Diagnostics;
using OpenTelemetry;

namespace Sddp.Api.Extensions;

/// <summary>MEAI emits error.Message even with EnableSensitiveData=false. Redact before export.</summary>
internal sealed class HarnessTelemetryProcessor : BaseProcessor<Activity>
{
    private static readonly HashSet<string> AllowedTags = new(StringComparer.Ordinal)
    {
        "gen_ai.operation.name", "gen_ai.provider.name", "gen_ai.system",
        "gen_ai.agent.id", "gen_ai.agent.name", "gen_ai.request.model", "gen_ai.response.model",
        "gen_ai.request.max_tokens", "gen_ai.request.temperature", "gen_ai.response.finish_reasons",
        "gen_ai.usage.input_tokens", "gen_ai.usage.output_tokens", "gen_ai.tool.name", "gen_ai.tool.type",
        "error.type"
    };

    public override void OnEnd(Activity activity)
    {
        if (activity.Source.Name != "Sddp.AI.MAF") return;
        if (activity.Status == ActivityStatusCode.Error) activity.SetStatus(ActivityStatusCode.Error);
        foreach (var tag in activity.TagObjects.ToArray())
            if (!AllowedTags.Contains(tag.Key)) activity.SetTag(tag.Key, null);
    }
}
