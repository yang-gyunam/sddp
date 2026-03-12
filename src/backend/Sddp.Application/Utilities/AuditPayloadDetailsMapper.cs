using System.Reflection;

namespace Sddp.Application.Utilities;

internal static class AuditPayloadDetailsMapper
{
    internal static Dictionary<string, object>? Map(object? payload)
    {
        if (payload is null)
        {
            return null;
        }

        if (payload is Dictionary<string, object> details)
        {
            return new Dictionary<string, object>(details, StringComparer.Ordinal);
        }

        if (payload is IReadOnlyDictionary<string, object> readOnlyDetails)
        {
            return readOnlyDetails.ToDictionary(
                pair => pair.Key,
                pair => pair.Value,
                StringComparer.Ordinal);
        }

        if (payload is IEnumerable<KeyValuePair<string, object>> enumerableDetails)
        {
            return enumerableDetails.ToDictionary(
                pair => pair.Key,
                pair => pair.Value,
                StringComparer.Ordinal);
        }

        var properties = payload.GetType()
            .GetProperties(BindingFlags.Instance | BindingFlags.Public)
            .Where(property => property.CanRead && property.GetIndexParameters().Length == 0);

        var mapped = new Dictionary<string, object>(StringComparer.Ordinal);
        foreach (var property in properties)
        {
            mapped[property.Name] = property.GetValue(payload)!;
        }

        return mapped;
    }
}
