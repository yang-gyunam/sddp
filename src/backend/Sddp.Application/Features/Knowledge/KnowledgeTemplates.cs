using System.Text.Json;

namespace Sddp.Application.Features.Knowledge;

public static class KnowledgeTemplates
{
    private const string EmptyDocument = """{"type":"doc","content":[]}""";

    private static readonly IReadOnlyDictionary<string, string[]> TemplateHeadings =
        new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
        {
            ["architecture-overview"] =
                ["Context", "Components", "Data Flow", "Decisions", "Related Specs"],
            ["onboarding"] =
                ["Prerequisites", "Setup", "First Task", "Team Contacts"],
            ["runbook"] =
                ["Trigger", "Preconditions", "Procedure", "Verification", "Rollback"],
            ["troubleshooting"] =
                ["Symptom", "Diagnosis", "Resolution", "Prevention"],
            ["guideline"] =
                ["Purpose", "Rules", "Examples", "Exceptions"],
            ["product-policy"] =
                ["Background", "Policy", "Scope", "Exceptions", "Related Requirements"]
        };

    public static JsonElement Create(string? template)
    {
        if (string.IsNullOrWhiteSpace(template)
            || template.Equals("blank", StringComparison.OrdinalIgnoreCase))
        {
            return Parse(EmptyDocument);
        }

        if (!TemplateHeadings.TryGetValue(template, out var headings))
        {
            throw new Sddp.Abstractions.Exceptions.ValidationException(
                "template",
                $"Unknown Knowledge template '{template}'.");
        }

        var nodes = headings.SelectMany(heading => new object[]
        {
            new
            {
                type = "heading",
                attrs = new { level = 2 },
                content = new[] { new { type = "text", text = heading } }
            },
            new
            {
                type = "paragraph",
                content = Array.Empty<object>()
            }
        });

        return JsonSerializer.SerializeToElement(new { type = "doc", content = nodes });
    }

    private static JsonElement Parse(string json)
    {
        using var document = JsonDocument.Parse(json);
        return document.RootElement.Clone();
    }
}
