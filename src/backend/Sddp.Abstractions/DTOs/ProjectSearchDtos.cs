namespace Sddp.Abstractions.DTOs;

/// <summary>One readable source and its domain navigation target.</summary>
public sealed record ProjectSearchHit(string Menu, string Key, string Target, string Kind, string Title,
    string Code, string Snippet, string Location, string Status, string Version, DateTimeOffset UpdatedAt, string? Revision, DateTimeOffset? CreatedAt, int? Line);

/// <summary>Ten globally ordered hits and counts before the menu filter.</summary>
public sealed record ProjectSearchPage(IReadOnlyList<ProjectSearchHit> Items, IReadOnlyDictionary<string, int> Counts,
    int? NextOffset, string? NextCursor, string[] Terms, bool FilesUnavailable);
