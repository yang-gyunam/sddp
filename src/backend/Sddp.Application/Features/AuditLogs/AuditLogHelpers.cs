using System.Text.Json;
using Sddp.Abstractions.DTOs;
using Sddp.Abstractions.ValueObjects;
using Sddp.Domain.Entities;

namespace Sddp.Application.Features.AuditLogs;

internal static class AuditLogHelpers
{
    internal static AuditLogDto MapToDto(AuditLog log, IReadOnlyDictionary<GlobalUniqueId, string> userMap)
    {
        var userName = "System";
        if (log.ActorId.HasValue && userMap.TryGetValue(log.ActorId.Value, out var name))
        {
            userName = name;
        }

        Dictionary<string, object>? details = null;
        if (!string.IsNullOrEmpty(log.Payload))
        {
            try
            {
                details = JsonSerializer.Deserialize<Dictionary<string, object>>(log.Payload);
            }
            catch
            {
                details = new Dictionary<string, object> { ["raw"] = log.Payload };
            }
        }

        return new AuditLogDto(
            Id: log.Id.ToString(),
            Timestamp: log.CreatedAt.ToIso8601(),
            UserId: log.ActorId?.ToString() ?? "",
            UserName: userName,
            Action: log.Action,
            ResourceType: log.ResourceType,
            ResourceId: log.ResourceId.ToString(),
            IpAddress: log.IpAddress ?? "",
            Details: details);
    }

    internal static List<FieldAuthorDto> BuildFieldAuthors(
        IEnumerable<AuditLog> orderedLogs,
        IReadOnlyDictionary<GlobalUniqueId, string> userMap,
        string resourceType = "spec")
    {
        var fieldAuthors = new Dictionary<string, FieldAuthorDto>();

        foreach (var log in orderedLogs)
        {
            if (string.IsNullOrEmpty(log.Payload) || !log.ActorId.HasValue)
            {
                continue;
            }

            var userName = userMap.TryGetValue(log.ActorId.Value, out var name) ? name : "Unknown";
            var timestamp = log.CreatedAt.ToIso8601();
            var userId = log.ActorId.Value.ToString();

            try
            {
                var payload = JsonSerializer.Deserialize<JsonElement>(log.Payload);

                if (payload.TryGetProperty("changes", out var changesElement)
                    && changesElement.ValueKind == JsonValueKind.Array)
                {
                    foreach (var change in changesElement.EnumerateArray())
                    {
                        if (change.TryGetProperty("field", out var fieldElement))
                        {
                            var fieldName = fieldElement.GetString();
                            if (!string.IsNullOrEmpty(fieldName) && !fieldAuthors.ContainsKey(fieldName))
                            {
                                fieldAuthors[fieldName] = new FieldAuthorDto(
                                    fieldName,
                                    userId,
                                    userName,
                                    timestamp);
                            }
                        }
                    }
                }

                if (log.Action == "create")
                {
                    var createFields = resourceType.Equals("glossary_term", StringComparison.OrdinalIgnoreCase)
                        ? new[] { "Term", "Definition", "Category", "Source", "Synonyms", "Abbreviation" }
                        : resourceType.Equals("requirement", StringComparison.OrdinalIgnoreCase)
                        ? new[] { "Title", "Description" }
                        : new[]
                        {
                            "Title", "Description", "Decision", "Context", "Scope",
                            "OutOfScope", "Definitions", "AcceptanceCriteria",
                            "Risks", "Assumptions", "Owners", "ReviewTrigger"
                        };

                    foreach (var field in createFields)
                    {
                        if (!fieldAuthors.ContainsKey(field))
                        {
                            fieldAuthors[field] = new FieldAuthorDto(
                                field,
                                userId,
                                userName,
                                timestamp);
                        }
                    }
                }
            }
            catch
            {
                // Ignore parse errors
            }
        }

        return fieldAuthors.Values.ToList();
    }
}
