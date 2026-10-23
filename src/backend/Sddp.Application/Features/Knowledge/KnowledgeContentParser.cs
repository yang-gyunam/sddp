using System.Text;
using System.Text.Json;
using Sddp.Abstractions.Enums;
using Sddp.Abstractions.Exceptions;
using Sddp.Abstractions.ValueObjects;

namespace Sddp.Application.Features.Knowledge;

public sealed record KnowledgeReference(
    KnowledgeTargetType TargetType,
    GlobalUniqueId TargetId);

public sealed record KnowledgeContentProjection(
    string Json,
    string PlainText,
    IReadOnlyList<KnowledgeReference> References);

public static class KnowledgeContentParser
{
    private const int MaximumContentBytes = 1_000_000;
    private const int MaximumNodes = 10_000;
    private const int MaximumDepth = 50;

    private static readonly HashSet<string> AllowedNodeTypes =
    [
        "doc", "paragraph", "text", "heading", "bulletList", "orderedList",
        "listItem", "blockquote", "codeBlock",
        "horizontalRule", "hardBreak", "table", "tableRow", "tableHeader",
        "tableCell", "sddpEntityMention"
    ];

    private static readonly HashSet<string> AllowedMarkTypes =
    [
        "bold", "italic", "strike", "code", "link"
    ];

    private static readonly HashSet<string> AllowedNodeProperties =
    [
        "type", "attrs", "content", "marks", "text"
    ];

    private static readonly HashSet<string> AllowedMarkProperties =
    [
        "type", "attrs"
    ];

    private static readonly IReadOnlyDictionary<string, HashSet<string>> AllowedNodeAttributes =
        new Dictionary<string, HashSet<string>>(StringComparer.Ordinal)
        {
            ["heading"] = ["level"],
            ["orderedList"] = ["start", "type"],
            ["codeBlock"] = ["language"],
            ["tableHeader"] = ["colspan", "rowspan", "colwidth", "align"],
            ["tableCell"] = ["colspan", "rowspan", "colwidth", "align"],
            ["sddpEntityMention"] = ["entityType", "entityId"]
        };

    private static readonly HashSet<string> EmptyAttributes = [];
    private static readonly HashSet<string> AllowedLinkAttributes =
    [
        "href", "target", "rel", "class"
    ];

    public static KnowledgeContentProjection Parse(JsonElement content)
    {
        if (content.ValueKind != JsonValueKind.Object)
        {
            throw new ValidationException("content", "Knowledge content must be a TipTap document object.");
        }

        if (!content.TryGetProperty("type", out var rootType)
            || rootType.ValueKind != JsonValueKind.String
            || rootType.GetString() != "doc")
        {
            throw new ValidationException("content", "Knowledge content root type must be 'doc'.");
        }

        var json = JsonSerializer.Serialize(content);
        if (Encoding.UTF8.GetByteCount(json) > MaximumContentBytes)
        {
            throw new ValidationException("content", "Knowledge content must not exceed 1 MB.");
        }

        var text = new StringBuilder();
        var references = new HashSet<KnowledgeReference>();
        var nodeCount = 0;
        ValidateNode(content, 0, ref nodeCount, text, references);

        var plainText = string.Join(
            ' ',
            text.ToString().Split(
                [' ', '\r', '\n', '\t'],
                StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));

        return new KnowledgeContentProjection(
            json,
            plainText,
            references
                .OrderBy(reference => reference.TargetType)
                .ThenBy(reference => reference.TargetId.ToString(), StringComparer.Ordinal)
                .ToArray());
    }

    public static IReadOnlyList<string> NormalizeTags(IReadOnlyList<string>? tags)
    {
        if (tags is null)
        {
            return [];
        }

        var normalized = tags
            .Select(tag => tag.Trim())
            .Where(tag => tag.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Order(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (normalized.Length > 20)
        {
            throw new ValidationException("tags", "A page can have at most 20 tags.");
        }

        if (normalized.Any(tag => tag.Length > 50))
        {
            throw new ValidationException("tags", "Each tag must not exceed 50 characters.");
        }

        return normalized;
    }

    public static string SerializeTags(IReadOnlyList<string> tags) =>
        JsonSerializer.Serialize(tags);

    public static IReadOnlyList<string> DeserializeTags(string json) =>
        JsonSerializer.Deserialize<string[]>(json) ?? [];

    private static void ValidateNode(
        JsonElement node,
        int depth,
        ref int nodeCount,
        StringBuilder text,
        HashSet<KnowledgeReference> references)
    {
        if (depth > MaximumDepth)
        {
            throw new ValidationException("content", "Knowledge content nesting is too deep.");
        }

        if (++nodeCount > MaximumNodes)
        {
            throw new ValidationException("content", "Knowledge content contains too many nodes.");
        }

        if (!node.TryGetProperty("type", out var typeElement)
            || typeElement.ValueKind != JsonValueKind.String)
        {
            throw new ValidationException("content", "Every TipTap node must have a type.");
        }

        var type = typeElement.GetString()!;
        if (!AllowedNodeTypes.Contains(type))
        {
            throw new ValidationException("content", $"Unsupported Knowledge node type '{type}'.");
        }

        ValidateObjectProperties(node, AllowedNodeProperties, "node");
        ValidateNodeAttributes(node, type);

        if (type == "text")
        {
            if (!node.TryGetProperty("text", out var textElement)
                || textElement.ValueKind != JsonValueKind.String)
            {
                throw new ValidationException("content", "Text nodes must contain text.");
            }

            text.Append(textElement.GetString()).Append(' ');
        }

        if (node.TryGetProperty("marks", out var marks))
        {
            ValidateMarks(marks);
        }

        if (type == "sddpEntityMention")
        {
            references.Add(ParseReference(node));
        }

        if (!node.TryGetProperty("content", out var children))
        {
            return;
        }

        if (children.ValueKind != JsonValueKind.Array)
        {
            throw new ValidationException("content", "TipTap node content must be an array.");
        }

        foreach (var child in children.EnumerateArray())
        {
            if (child.ValueKind != JsonValueKind.Object)
            {
                throw new ValidationException("content", "TipTap child nodes must be objects.");
            }

            ValidateNode(child, depth + 1, ref nodeCount, text, references);
        }
    }

    private static void ValidateMarks(JsonElement marks)
    {
        if (marks.ValueKind != JsonValueKind.Array)
        {
            throw new ValidationException("content", "TipTap marks must be an array.");
        }

        foreach (var mark in marks.EnumerateArray())
        {
            if (mark.ValueKind != JsonValueKind.Object)
            {
                throw new ValidationException("content", "TipTap marks must be objects.");
            }

            ValidateObjectProperties(mark, AllowedMarkProperties, "mark");
            if (!mark.TryGetProperty("type", out var typeElement)
                || typeElement.ValueKind != JsonValueKind.String
                || !AllowedMarkTypes.Contains(typeElement.GetString()!))
            {
                throw new ValidationException("content", "Knowledge content contains an unsupported mark.");
            }

            if (typeElement.GetString() == "link")
            {
                ValidateLink(mark);
            }
            else if (mark.TryGetProperty("attrs", out var attrs)
                && attrs.ValueKind == JsonValueKind.Object
                && attrs.EnumerateObject().Any())
            {
                throw new ValidationException(
                    "content",
                    $"Mark '{typeElement.GetString()}' does not support attributes.");
            }
        }
    }

    private static void ValidateLink(JsonElement mark)
    {
        if (!mark.TryGetProperty("attrs", out var attrs)
            || attrs.ValueKind != JsonValueKind.Object
            || !attrs.TryGetProperty("href", out var hrefElement)
            || hrefElement.ValueKind != JsonValueKind.String)
        {
            throw new ValidationException("content", "Link marks must contain an href.");
        }

        ValidateObjectProperties(
            attrs,
            AllowedLinkAttributes,
            "link attribute");
        var href = hrefElement.GetString()!.Trim();
        var isAllowed = href.StartsWith("/", StringComparison.Ordinal)
            || href.StartsWith("#", StringComparison.Ordinal)
            || Uri.TryCreate(href, UriKind.Absolute, out var uri)
            && (uri.Scheme == Uri.UriSchemeHttp
                || uri.Scheme == Uri.UriSchemeHttps
                || uri.Scheme == Uri.UriSchemeMailto);
        if (!isAllowed)
        {
            throw new ValidationException("content", "Only http, https, mailto, relative, and anchor links are allowed.");
        }

        if (attrs.TryGetProperty("target", out var target)
            && target.ValueKind is not JsonValueKind.Null
            && (target.ValueKind != JsonValueKind.String
                || target.GetString() is not ("_blank" or "_self")))
        {
            throw new ValidationException("content", "Link target must be _blank, _self, or null.");
        }

        foreach (var optionalName in new[] { "rel", "class" })
        {
            if (attrs.TryGetProperty(optionalName, out var optional)
                && optional.ValueKind is not JsonValueKind.Null
                && optional.ValueKind != JsonValueKind.String)
            {
                throw new ValidationException(
                    "content",
                    $"Link {optionalName} must be a string or null.");
            }
        }
    }

    private static void ValidateNodeAttributes(JsonElement node, string type)
    {
        if (!node.TryGetProperty("attrs", out var attrs))
        {
            return;
        }

        if (attrs.ValueKind != JsonValueKind.Object)
        {
            throw new ValidationException("content", $"Node '{type}' attributes must be an object.");
        }

        var allowed = AllowedNodeAttributes.TryGetValue(type, out var configured)
            ? configured
            : EmptyAttributes;
        ValidateObjectProperties(attrs, allowed, $"{type} attribute");

        switch (type)
        {
            case "heading":
                if (!attrs.TryGetProperty("level", out var level)
                    || !level.TryGetInt32(out var headingLevel)
                    || headingLevel is < 1 or > 3)
                {
                    throw new ValidationException("content", "Heading level must be between 1 and 3.");
                }

                break;

            case "orderedList":
                if (attrs.TryGetProperty("type", out var listType)
                    && listType.ValueKind != JsonValueKind.Null
                    && (listType.ValueKind != JsonValueKind.String
                        || listType.GetString() is not ("1" or "a" or "A" or "i" or "I")))
                {
                    throw new ValidationException(
                        "content", "Ordered list type must be null or one of: 1, a, A, i, I.");
                }

                if (attrs.TryGetProperty("start", out var start)
                    && (!start.TryGetInt32(out var startValue) || startValue < 1))
                {
                    throw new ValidationException("content", "Ordered list start must be a positive integer.");
                }

                break;

            case "codeBlock":
                if (attrs.TryGetProperty("language", out var language)
                    && language.ValueKind is not JsonValueKind.Null
                    && language.ValueKind != JsonValueKind.String)
                {
                    throw new ValidationException("content", "Code block language must be a string or null.");
                }

                break;

            case "tableHeader":
            case "tableCell":
                ValidateTableCellAttributes(attrs);
                break;
        }
    }

    private static void ValidateTableCellAttributes(JsonElement attrs)
    {
        if (attrs.TryGetProperty("align", out var align)
            && align.ValueKind != JsonValueKind.Null
            && (align.ValueKind != JsonValueKind.String
                || align.GetString() is not ("left" or "center" or "right")))
        {
            throw new ValidationException(
                "content", "Table cell align must be null, left, center, or right.");
        }

        foreach (var name in new[] { "colspan", "rowspan" })
        {
            if (attrs.TryGetProperty(name, out var span)
                && (!span.TryGetInt32(out var spanValue) || spanValue is < 1 or > 100))
            {
                throw new ValidationException(
                    "content",
                    $"Table cell {name} must be between 1 and 100.");
            }
        }

        if (!attrs.TryGetProperty("colwidth", out var colwidth)
            || colwidth.ValueKind == JsonValueKind.Null)
        {
            return;
        }

        if (colwidth.ValueKind != JsonValueKind.Array
            || colwidth.EnumerateArray().Any(
                width => !width.TryGetInt32(out var value) || value < 1))
        {
            throw new ValidationException(
                "content",
                "Table cell colwidth must be null or an array of positive integers.");
        }
    }

    private static void ValidateObjectProperties(
        JsonElement value,
        IReadOnlySet<string> allowed,
        string subject)
    {
        foreach (var property in value.EnumerateObject())
        {
            if (!allowed.Contains(property.Name))
            {
                throw new ValidationException(
                    "content",
                    $"Unsupported {subject} '{property.Name}'.");
            }
        }
    }

    private static KnowledgeReference ParseReference(JsonElement node)
    {
        if (!node.TryGetProperty("attrs", out var attrs)
            || !attrs.TryGetProperty("entityType", out var typeElement)
            || typeElement.ValueKind != JsonValueKind.String
            || !attrs.TryGetProperty("entityId", out var idElement)
            || idElement.ValueKind != JsonValueKind.String
            || !Enum.TryParse<KnowledgeTargetType>(typeElement.GetString(), true, out var targetType)
            || !GlobalUniqueId.TryParse(idElement.GetString(), out var targetId))
        {
            throw new ValidationException(
                "content",
                "SDDP entity mentions require a valid entityType and entityId.");
        }

        return new KnowledgeReference(targetType, targetId);
    }
}
