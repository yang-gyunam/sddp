namespace Sddp.Abstractions.DTOs;

/// <summary>A committed repository entry. Kind is directory, file, symlink or submodule.</summary>
public sealed record ProjectFileEntryDto(string Name, string Path, string Kind, long? SizeBytes);

/// <summary>One directory at an immutable revision of the configured branch.</summary>
public sealed record ProjectFileTreeDto(string Branch, string Commit, string Path, IReadOnlyList<ProjectFileEntryDto> Entries);

/// <summary>One page of matching committed files across the entire repository.</summary>
public sealed record ProjectFileSearchDto(string Branch, string Commit, IReadOnlyList<ProjectFileEntryDto> Entries,
    int TotalCount, int? NextSkip);

/// <summary>A file change, including its path at that commit (before later renames).</summary>
public sealed record ProjectFileCommitDto(string Commit, string Author, DateTimeOffset AuthoredAt, string Message, string Path);

/// <summary>Bounded, newest-first file history.</summary>
public sealed record ProjectFileHistoryDto(string Commit, IReadOnlyList<ProjectFileCommitDto> Items, int? NextSkip);

/// <summary>Read-only Git blob. Content is null for binary, oversized or submodule entries.</summary>
public sealed record ProjectFileContentDto(string Branch, string Commit, string Path, string Kind,
    long SizeBytes, string? Content, bool IsBinary, bool TooLarge, long MaxBytes, ProjectFileCommitDto? LastChange);

/// <summary>A current SDDP entity explicitly linked to the file.</summary>
public sealed record ProjectFileLinkDto(string Id, string Type, string Title, string? Code = null);

/// <summary>Current SDDP owners and relationships; these are not historical Git metadata.</summary>
public sealed record ProjectFileRelationsDto(IReadOnlyList<UserRefDto> Owners, IReadOnlyList<ProjectFileLinkDto> Items);
