using System.Text.Json;
using System.Text.Json.Serialization;
using Sddp.Abstractions.ValueObjects;

namespace Sddp.Infrastructure.Serialization;

public sealed class TimestampJsonConverter : JsonConverter<Timestamp>
{
    public override Timestamp Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
        {
            return Timestamp.Empty;
        }

        if (reader.TokenType != JsonTokenType.String)
        {
            throw new JsonException("Timestamp must be an ISO 8601 string.");
        }

        var raw = reader.GetString();
        if (string.IsNullOrWhiteSpace(raw))
        {
            return Timestamp.Empty;
        }

        if (Timestamp.TryParse(raw, out var value))
        {
            return value;
        }

        throw new JsonException($"Invalid Timestamp value: '{raw}'.");
    }

    public override void Write(Utf8JsonWriter writer, Timestamp value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToIso8601());
    }
}
