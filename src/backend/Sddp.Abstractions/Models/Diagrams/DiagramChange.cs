namespace Sddp.Abstractions.Models.Diagrams;

public sealed record DiagramChange(
    string Target,
    string Id,
    string? Field,
    string Kind,
    string Category,
    object? Before,
    object? After);
