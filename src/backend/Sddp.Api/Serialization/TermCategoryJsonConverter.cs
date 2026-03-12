using System.Text.Json;
using System.Text.Json.Serialization;
using Sddp.Abstractions.Enums;

namespace Sddp.Api.Serialization;

public sealed class TermCategoryJsonConverter : JsonConverter<TermCategory>
{
    private static readonly Dictionary<string, TermCategory> NameToCategory = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Technical"] = TermCategory.Technical,
        ["Business"] = TermCategory.Business,
        ["Abbreviation"] = TermCategory.Abbreviation,
        ["Domain"] = TermCategory.Domain,
        ["Architecture"] = TermCategory.Architecture,
        ["Infrastructure"] = TermCategory.Infrastructure,
        ["Security"] = TermCategory.Security,
        ["Compliance"] = TermCategory.Compliance,
        ["DesignPattern"] = TermCategory.DesignPattern,
        ["Design Pattern"] = TermCategory.DesignPattern,
        ["Design-Pattern"] = TermCategory.DesignPattern,
        ["Design_Pattern"] = TermCategory.DesignPattern,
    };

    public override TermCategory Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.String)
        {
            var raw = reader.GetString();
            if (string.IsNullOrWhiteSpace(raw))
            {
                throw new JsonException("TermCategory value is empty.");
            }

            if (NameToCategory.TryGetValue(raw, out var category))
            {
                return category;
            }

            var normalized = raw.Replace(" ", string.Empty)
                .Replace("-", string.Empty)
                .Replace("_", string.Empty);

            if (NameToCategory.TryGetValue(normalized, out category))
            {
                return category;
            }

            if (Enum.TryParse<TermCategory>(raw, ignoreCase: true, out var parsed))
            {
                return parsed;
            }

            throw new JsonException($"Invalid TermCategory value: '{raw}'.");
        }

        if (reader.TokenType == JsonTokenType.Number && reader.TryGetInt32(out var numeric))
        {
            return (TermCategory)numeric;
        }

        throw new JsonException("Invalid TermCategory value.");
    }

    public override void Write(Utf8JsonWriter writer, TermCategory value, JsonSerializerOptions options)
    {
        var output = value switch
        {
            TermCategory.DesignPattern => "Design Pattern",
            _ => value.ToString(),
        };

        writer.WriteStringValue(output);
    }
}
