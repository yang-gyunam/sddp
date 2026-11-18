using Sddp.Abstractions.Models.Diagrams;
using YamlDotNet.Core;
using YamlDotNet.Core.Events;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using YamlDotNet.Serialization.Utilities;

namespace Sddp.Infrastructure.Services.Diagrams;

internal static class DiagramYamlReader
{
    private const string StringTag = "tag:yaml.org,2002:str";
    private const string IntegerTag = "tag:yaml.org,2002:int";
    private const string MappingTag = "tag:yaml.org,2002:map";

    private static readonly IDeserializer Reader = Deserializer.FromValueDeserializer(new CheckedAliases(
        new DeserializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .WithDuplicateKeyChecking()
            .WithEnforceRequiredMembers()
            .WithEnforceNullability()
            .WithNodeTypeResolver(new FieldTypes())
            .BuildValueDeserializer()));

    public static DiagramRevision Read(string yaml) =>
        Reader.Deserialize<DiagramRevision>(new SchemaParser(new Parser(new StringReader(yaml))))
        ?? throw new InvalidDataException("Expected a diagram revision.");

    // Retain the schema's CLR type. The default tag resolver would first read a
    // tagged float and then round it to the integer requested by the DTO.
    private sealed class FieldTypes : INodeTypeResolver
    {
        public bool Resolve(NodeEvent? node, ref Type currentType)
        {
            if (node is null)
                return false;
            var expected = currentType == typeof(int) ? IntegerTag
                : currentType == typeof(string) ? StringTag : MappingTag;
            if (expected == MappingTag ? node is not MappingStart : node is not Scalar)
                throw new YamlException(node.Start, node.End, $"Expected {expected} for {currentType.Name}.");
            if (!node.Tag.IsEmpty && node.Tag != expected)
                throw new YamlException(node.Start, node.End, $"Unexpected tag '{node.Tag}' for {currentType.Name}.");
            if (expected == IntegerTag && node is Scalar scalar && scalar.Tag.IsEmpty && scalar.Style != ScalarStyle.Plain)
                throw new YamlException(node.Start, node.End, "Expected an integer, found a string.");
            return true;
        }
    }

    // DTO property names bypass the type resolver. Also mark explicitly tagged
    // strings as strings before YamlDotNet's plain-null handling runs.
    private sealed class SchemaParser(IParser parser) : IParser
    {
        public ParsingEvent? Current { get; private set; }

        public bool MoveNext()
        {
            var moved = parser.MoveNext();
            Current = parser.Current;
            if (Current is not Scalar scalar)
                return moved;
            if (scalar.IsKey && !scalar.Tag.IsEmpty && scalar.Tag != StringTag)
                throw new YamlException(scalar.Start, scalar.End, $"Unexpected tag '{scalar.Tag}' on a diagram key.");
            if (scalar.IsKey && scalar.Tag.IsEmpty && scalar.Style == ScalarStyle.Plain
                && scalar.Value is "" or "~" or "null" or "Null" or "NULL")
                throw new YamlException(scalar.Start, scalar.End, "Expected a string key, found null.");
            if (scalar.Tag == StringTag && scalar.Style == ScalarStyle.Plain)
                Current = new Scalar(scalar.Anchor, scalar.Tag, scalar.Value, ScalarStyle.DoubleQuoted,
                    false, false, scalar.Start, scalar.End, scalar.IsKey);
            return moved;
        }
    }

    // Aliases return an already constructed object without invoking FieldTypes.
    private sealed class CheckedAliases(IValueDeserializer inner) : IValueDeserializer
    {
        public object? DeserializeValue(IParser parser, Type expectedType, SerializerState state, IValueDeserializer nested)
        {
            var alias = parser.Current as AnchorAlias;
            var value = inner.DeserializeValue(parser, expectedType, state, nested);
            if (alias is not null && !expectedType.IsInstanceOfType(value))
                throw new YamlException(alias.Start, alias.End, $"Alias '{alias.Value}' does not match {expectedType.Name}.");
            return value;
        }
    }
}
