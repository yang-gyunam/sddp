using System.Collections;
using System.Globalization;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using Sddp.Abstractions.Models.Diagrams.V2;
using YamlDotNet.Core;
using YamlDotNet.RepresentationModel;

namespace Sddp.Infrastructure.Services.Diagrams;

/// <summary>Strict typed schema2 YAML. Temporary YAML/JSON trees never become stored semantic bags.</summary>
public static class DiagramYaml2
{
    private const string Prefix = "tag:yaml.org,2002:";
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public static DiagramRevision2 Read(string yaml)
    {
        var stream = new YamlStream();
        stream.Load(new StringReader(yaml));
        if (stream.Documents.Count != 1) throw new InvalidDataException("Expected exactly one YAML document.");
        var root = stream.Documents[0].RootNode;
        var value = ConvertNode(root, typeof(DiagramRevision2), false, new HashSet<YamlNode>(ReferenceEqualityComparer.Instance), 0);
        var model = value!.Deserialize<DiagramRevision2>(Json) ?? throw new InvalidDataException("Expected schema2 model.");
        DiagramValidation2.Validate(model);
        return model;
    }

    public static DiagramRevision2 ReadAny(string yaml, out int inputSchemaVersion)
    {
        var stream = new YamlStream(); stream.Load(new StringReader(yaml));
        if (stream.Documents.Count != 1 || stream.Documents[0].RootNode is not YamlMappingNode root)
            throw new InvalidDataException("Expected one diagram mapping.");
        var entries = Entries(root);
        if (!entries.TryGetValue("schemaVersion", out var version)) throw new InvalidDataException("Expected schemaVersion.");
        var scalar = Scalar(version, "int");
        if (scalar.Tag.IsEmpty && scalar.Style != ScalarStyle.Plain || !int.TryParse(scalar.Value, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out inputSchemaVersion))
            throw new InvalidDataException("Expected integer schemaVersion.");
        return inputSchemaVersion switch { 1 => DiagramBridge2.Promote(DiagramYaml.Read(yaml)), 2 => Read(yaml), _ => throw new InvalidDataException("UNSUPPORTED_SCHEMA") };
    }

    public static string Write(DiagramRevision2 model)
    {
        DiagramValidation2.Validate(model);
        var root = JsonSerializer.SerializeToNode(model, Json)!;
        var stream = new YamlStream(new YamlDocument(Emit(root)));
        using var text = new StringWriter(CultureInfo.InvariantCulture);
        stream.Save(text, false);
        return text.ToString();
    }

    private static bool IsNull(YamlNode node) => node is YamlScalarNode scalar
        && (scalar.Tag == Prefix + "null" || scalar.Tag.IsEmpty && scalar.Style == ScalarStyle.Plain
            && scalar.Value is null or "" or "~" or "null" or "Null" or "NULL");

    private static JsonNode? ConvertNode(YamlNode node, Type type, bool nullable, HashSet<YamlNode> stack, int depth)
    {
        if (depth > 64 || !stack.Add(node)) throw new InvalidDataException("Cyclic or excessively nested YAML.");
        try
        {
            if (IsNull(node))
            {
                if (!nullable) throw new InvalidDataException($"Expected non-null {type.Name}.");
                return null;
            }
            if (type == typeof(string)) return JsonValue.Create(String(node));
            if (type == typeof(int))
            {
                var s = Scalar(node, "int");
                if (s.Tag.IsEmpty && s.Style != ScalarStyle.Plain || !int.TryParse(s.Value, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out var number))
                    throw new InvalidDataException("Expected integer scalar.");
                return JsonValue.Create(number);
            }
            if (type == typeof(bool))
            {
                var s = Scalar(node, "bool");
                if (s.Tag.IsEmpty && s.Style != ScalarStyle.Plain || !bool.TryParse(s.Value, out var boolean))
                    throw new InvalidDataException("Expected boolean scalar.");
                return JsonValue.Create(boolean);
            }
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>))
            {
                CheckTag(node, "seq");
                if (node is not YamlSequenceNode sequence) throw new InvalidDataException("Expected sequence.");
                return new JsonArray(sequence.Children.Select(n => ConvertNode(n, type.GenericTypeArguments[0], false, stack, depth + 1)).ToArray());
            }
            CheckTag(node, "map");
            if (node is not YamlMappingNode mapping) throw new InvalidDataException("Expected mapping.");
            var entries = Entries(mapping);
            if (type == typeof(DiagramContent2)) throw new InvalidDataException("Content requires diagramType.");
            if (type == typeof(SequenceEvent2)) type = RequiredString(entries, "type") switch
            {
                "message" => typeof(SequenceMessage2), "activationStart" => typeof(SequenceActivationStart2),
                "activationEnd" => typeof(SequenceActivationEnd2), "fragment" => typeof(SequenceFragment2),
                _ => throw new InvalidDataException("Unknown sequence event type.")
            };
            var result = new JsonObject();
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Dictionary<,>))
            {
                foreach (var (key, child) in entries) result.Add(key, ConvertNode(child, type.GenericTypeArguments[1], false, stack, depth + 1));
                return result;
            }
            var properties = type.GetProperties().ToDictionary(p => JsonNamingPolicy.CamelCase.ConvertName(p.Name), StringComparer.Ordinal);
            var nullability = new NullabilityInfoContext();
            foreach (var (key, child) in entries)
            {
                if (key == "type" && typeof(SequenceEvent2).IsAssignableFrom(type)) { result.Add(key, String(child)); continue; }
                if (!properties.TryGetValue(key, out var field)) throw new InvalidDataException($"Unknown {type.Name} field '{key}'.");
                var fieldType = field.PropertyType;
                if (type == typeof(DiagramRevision2) && key == "content") fieldType = ContentType(RequiredString(entries, "diagramType"));
                result.Add(key, ConvertNode(child, fieldType, nullability.Create(field).ReadState == NullabilityState.Nullable, stack, depth + 1));
            }
            foreach (var field in properties.Keys)
                if (!entries.ContainsKey(field)) throw new InvalidDataException($"Missing {type.Name} field '{field}'.");
            return result;
        }
        finally { stack.Remove(node); }
    }

    private static Type ContentType(string value) => value switch
    {
        "graph" => typeof(GraphContent2), "flowchart" => typeof(FlowchartContent2),
        "sequence" => typeof(SequenceContent2), "class" => typeof(ClassContent2),
        "state" => typeof(StateContent2), "er" => typeof(ErContent2), "c4" => typeof(C4Content2),
        _ => throw new InvalidDataException("Unknown diagramType.")
    };
    private static Dictionary<string, YamlNode> Entries(YamlMappingNode map)
    {
        var entries = new Dictionary<string, YamlNode>(StringComparer.Ordinal);
        foreach (var (key, value) in map.Children)
            if (!entries.TryAdd(String(key), value)) throw new InvalidDataException("Duplicate YAML key.");
        return entries;
    }
    private static string RequiredString(Dictionary<string, YamlNode> entries, string key) =>
        entries.TryGetValue(key, out var node) ? String(node) : throw new InvalidDataException($"Missing '{key}'.");
    private static string String(YamlNode node)
    {
        if (IsNull(node)) throw new InvalidDataException("Expected string, found null.");
        return Scalar(node, "str").Value ?? "";
    }
    private static YamlScalarNode Scalar(YamlNode node, string tag)
    {
        CheckTag(node, tag);
        return node as YamlScalarNode ?? throw new InvalidDataException($"Expected {tag} scalar.");
    }
    private static void CheckTag(YamlNode node, string tag)
    {
        if (!node.Tag.IsEmpty && node.Tag != "?" && node.Tag != Prefix + tag) throw new InvalidDataException($"Unexpected YAML tag '{node.Tag}'.");
    }
    private static YamlNode Emit(JsonNode? node)
    {
        if (node is null) return new YamlScalarNode("null") { Tag = Prefix + "null", Style = ScalarStyle.Plain };
        if (node is JsonObject map)
        {
            var result = new YamlMappingNode();
            foreach (var (key, value) in map) result.Add(new YamlScalarNode(key) { Style = ScalarStyle.DoubleQuoted }, Emit(value));
            return result;
        }
        if (node is JsonArray list) return new YamlSequenceNode(list.Select(Emit));
        var scalar = (JsonValue)node;
        if (scalar.TryGetValue<string>(out var text)) return new YamlScalarNode(text) { Style = ScalarStyle.DoubleQuoted };
        return new YamlScalarNode(node.ToJsonString()) { Style = ScalarStyle.Plain };
    }
}
