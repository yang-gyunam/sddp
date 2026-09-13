using System.Globalization;
using Sddp.Abstractions.Interfaces;
using Sddp.Abstractions.Models;
using YamlDotNet.Core;
using YamlDotNet.Core.Events;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.EventEmitters;
using YamlDotNet.Serialization.NamingConventions;

namespace Sddp.Infrastructure.Services;

public sealed class YamlArtifactDescriptorSerializer : IArtifactDescriptorSerializer
{
    private static readonly ISerializer Serializer = new SerializerBuilder()
        .WithNamingConvention(CamelCaseNamingConvention.Instance)
        .ConfigureDefaultValuesHandling(DefaultValuesHandling.OmitNull)
        .WithTypeConverter(new Iso8601DateTimeOffsetConverter())
        .WithEventEmitter(next => new MultilineLiteralEmitter(next))
        .Build();

    private static readonly IDeserializer Deserializer = new DeserializerBuilder()
        .WithNamingConvention(CamelCaseNamingConvention.Instance)
        .WithTypeConverter(new Iso8601DateTimeOffsetConverter())
        // Unknown keys are tolerated so a descriptor written by a newer schema version
        // still parses rather than throwing.
        .IgnoreUnmatchedProperties()
        .Build();

    public string Serialize(ArtifactDescriptor descriptor)
    {
        ArgumentNullException.ThrowIfNull(descriptor);
        return Serializer.Serialize(descriptor);
    }

    public ArtifactDescriptor Deserialize(string yaml)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(yaml);
        return Deserializer.Deserialize<ArtifactDescriptor>(yaml)
            ?? throw new YamlException("Descriptor YAML deserialized to null");
    }

    /// <summary>
    /// Emits <see cref="DateTimeOffset"/> as a single ISO-8601 UTC scalar.
    ///
    /// Without this, YamlDotNet reflects over the struct and writes twenty-odd keys
    /// (dayOfYear, ticks, offset…) that do not deserialize back and that leak the server's
    /// local timezone into a file meant to be read and diffed.
    /// </summary>
    private sealed class Iso8601DateTimeOffsetConverter : IYamlTypeConverter
    {
        // Capital F drops trailing zeros and takes the decimal point with them, so a whole
        // second still writes as 2026-08-02T01:00:00Z while a DB timestamp keeps its precision.
        // Truncating instead would make the file disagree with the record it projects, and the
        // round-trip test could not see it while its fixture sat on a whole second.
        private const string Format = "yyyy-MM-ddTHH:mm:ss.FFFFFFFZ";

        public bool Accepts(Type type) =>
            type == typeof(DateTimeOffset) || type == typeof(DateTimeOffset?);

        public object? ReadYaml(IParser parser, Type type, ObjectDeserializer rootDeserializer)
        {
            var scalar = parser.Consume<Scalar>();

            if (string.IsNullOrWhiteSpace(scalar.Value))
            {
                return type == typeof(DateTimeOffset?) ? null : default(DateTimeOffset);
            }

            return DateTimeOffset.Parse(
                scalar.Value,
                CultureInfo.InvariantCulture,
                DateTimeStyles.RoundtripKind);
        }

        public void WriteYaml(IEmitter emitter, object? value, Type type, ObjectSerializer serializer)
        {
            ArgumentNullException.ThrowIfNull(emitter);

            if (value is not DateTimeOffset timestamp)
            {
                emitter.Emit(new Scalar(string.Empty));
                return;
            }

            emitter.Emit(new Scalar(
                timestamp.ToUniversalTime().ToString(Format, CultureInfo.InvariantCulture)));
        }
    }

    /// <summary>
    /// Emits multi-line strings as literal blocks. Spec context and decision text are the
    /// part a human or agent actually reads; quoted-and-escaped one-liners would defeat
    /// the point of writing YAML at all.
    /// </summary>
    private sealed class MultilineLiteralEmitter : ChainedEventEmitter
    {
        public MultilineLiteralEmitter(IEventEmitter nextEmitter) : base(nextEmitter)
        {
        }

        public override void Emit(ScalarEventInfo eventInfo, IEmitter emitter)
        {
            ArgumentNullException.ThrowIfNull(eventInfo);

            if (eventInfo.Source.Value is string value && value.Contains('\n', StringComparison.Ordinal))
            {
                eventInfo.Style = ScalarStyle.Literal;
            }

            base.Emit(eventInfo, emitter);
        }
    }
}
