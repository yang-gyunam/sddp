namespace Sddp.Domain.Enums;

/// <summary>
/// Reason for existing code to Spec mapping
/// </summary>
public enum MappingReason
{
    /// <summary>
    /// Manually mapped by developer
    /// </summary>
    Manual = 0,

    /// <summary>
    /// Automatically detected by system (pattern matching, etc.)
    /// </summary>
    AutoDetected = 1,

    /// <summary>
    /// Suggested by AI
    /// </summary>
    AISuggested = 2
}
