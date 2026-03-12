using System.Text.Json;
using System.Text.Json.Serialization;
using Sddp.Abstractions.ValueObjects;

namespace Sddp.Infrastructure.Serialization;

public sealed class GlobalUniqueIdJsonConverter : JsonConverter<GlobalUniqueId>
{
    public override GlobalUniqueId Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
        {
            return GlobalUniqueId.Empty;
        }

        if (reader.TokenType != JsonTokenType.String)
        {
            throw new JsonException("GlobalUniqueId must be a string.");
        }

        var raw = reader.GetString();
        if (string.IsNullOrWhiteSpace(raw))
        {
            return GlobalUniqueId.Empty;
        }

        if (GlobalUniqueId.TryParse(raw, out var value))
        {
            return value;
        }

        throw new JsonException($"Invalid GlobalUniqueId value: '{raw}'.");
    }

    public override void Write(Utf8JsonWriter writer, GlobalUniqueId value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToString());
    }
}
