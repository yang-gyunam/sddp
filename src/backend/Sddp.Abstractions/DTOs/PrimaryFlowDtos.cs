namespace Sddp.Abstractions.DTOs;

/// <summary>
/// Primary Flow DTO
/// Conversation → Requirement → Spec → GlossaryTerm / Artifact
/// </summary>
public record PrimaryFlowNodeDto(
    string Id,
    string EntityType,
    string Label,
    string? Code,
    int Depth,
    bool IsCurrent);
