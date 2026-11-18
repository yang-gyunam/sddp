using Sddp.Abstractions.Models.Diagrams;
using YamlDotNet.Core;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.EventEmitters;
using YamlDotNet.Serialization.NamingConventions;

namespace Sddp.Infrastructure.Services.Diagrams;

public static class DiagramYaml
{
    private static readonly ISerializer Writer = new SerializerBuilder()
        .WithNamingConvention(CamelCaseNamingConvention.Instance)
        .WithQuotingNecessaryStrings()
        .WithEventEmitter(next => new PreserveLineEndings(next))
        .DisableAliases()
        .Build();

    // Literal block scalars normalize line breaks. Double-quote multiline strings
    // so a revision retains the exact source characters, including trailing CRLF.
    private sealed class PreserveLineEndings(IEventEmitter next) : ChainedEventEmitter(next)
    {
        public override void Emit(ScalarEventInfo info, IEmitter emitter)
        {
            if (info.Source.Value is string value && value.IndexOfAny(['\r', '\n', '\u0085', '\u2028', '\u2029']) >= 0)
                info.Style = ScalarStyle.DoubleQuoted;
            base.Emit(info, emitter);
        }
    }

    public static DiagramRevision Read(string yaml)
    {
        var model = DiagramYamlReader.Read(yaml);
        DiagramValidation.Validate(model);
        return model;
    }

    public static string Write(DiagramRevision model)
    {
        DiagramValidation.Validate(model);
        return Writer.Serialize(model);
    }
}
