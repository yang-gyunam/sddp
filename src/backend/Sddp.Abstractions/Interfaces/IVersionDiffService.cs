using Sddp.Abstractions.ValueObjects;

namespace Sddp.Abstractions.Interfaces;

/// <summary>
///
/// REQ-01.4:
/// </summary>
public interface IVersionDiffService
{
    /// <summary>
    /// Spec
    /// </summary>
    Task<SpecDiffResultDto> CompareSpecVersionsAsync(
        GlobalUniqueId tenantId,
        GlobalUniqueId projectId,
        GlobalUniqueId specId1,
        GlobalUniqueId specId2,
        CancellationToken cancellationToken = default);
}

#region Service DTOs

/// <summary>
/// Diff change type
/// </summary>
public enum DiffChangeType
{
    /// <summary>change </summary>
    Unchanged = 0,
    /// <summary></summary>
    Added = 1,
    /// <summary>delete</summary>
    Removed = 2,
    /// <summary>update</summary>
    Modified = 3
}

/// <summary>
/// Diff DTO
/// </summary>
public record FieldDiffDto(
    string FieldName,
    string FieldLabel,
    string? OldValue,
    string? NewValue,
    DiffChangeType ChangeType);

/// <summary>
/// JSON Diff Operation DTO (JSON Patch)
/// </summary>
public record JsonDiffOperationDto(
    string Op,
    string Path,
    string? ValueJson = null,
    string? From = null);

/// <summary>
/// JSON Diff DTO
/// </summary>
public record JsonDiffResultDto(
    IReadOnlyList<JsonDiffOperationDto> Operations);

/// <summary>
/// Spec Diff DTO
/// </summary>
public record SpecDiffResultDto(
    string Spec1Id,
    string Spec1Version,
    string Spec2Id,
    string Spec2Version,
    IEnumerable<FieldDiffDto> Changes,
    int AddedCount,
    int RemovedCount,
    int ModifiedCount,
    JsonDiffResultDto? JsonDiff = null);

#endregion
