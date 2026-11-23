using System.Security.Cryptography;
using System.Text.Json;
using Sddp.Abstractions.Exceptions;

namespace Sddp.Application.Features.ProjectSearch;

public sealed record ProjectSearchCursor(string Fingerprint, int Score, DateTimeOffset Time, string Menu, string Key)
{
    public static string Identify(params string[] parts) =>
        Convert.ToHexString(SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(parts)));

    public string Encode() => Convert.ToBase64String(JsonSerializer.SerializeToUtf8Bytes(this));

    public static ProjectSearchCursor? Decode(string? encoded, string fingerprint)
    {
        if (encoded is null) return null;
        if (encoded.Length <= 12_000)
        {
            try
            {
                var cursor = JsonSerializer.Deserialize<ProjectSearchCursor>(Convert.FromBase64String(encoded));
                if (cursor is { Score: >= 0 and <= 3 } && cursor.Fingerprint == fingerprint &&
                    !string.IsNullOrEmpty(cursor.Key) && ProjectSearchQueryHandler.Menus.Contains(cursor.Menu)) return cursor;
            }
            catch (FormatException) { }
            catch (JsonException) { }
        }
        throw new ValidationException("cursor", "Invalid cursor for this project search.");
    }
}
