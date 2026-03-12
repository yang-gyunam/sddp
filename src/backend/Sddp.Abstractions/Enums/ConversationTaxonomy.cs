namespace Sddp.Abstractions.Enums;

/// <summary>
/// Canonical conversation type.
/// </summary>
public enum ConversationType
{
    Channel = 0,
    Forum = 1,
    DirectMessage = 2
}

/// <summary>
/// Canonical conversation visibility.
/// </summary>
public enum ConversationVisibility
{
    Public = 0,
    Private = 1
}

/// <summary>
/// Canonical conversation scope.
/// </summary>
public enum ConversationScope
{
    TenantWide = 0,
    ProjectScoped = 1
}

public static class ConversationTaxonomyExtensions
{
    /// <summary>
    /// Converts legacy private flag to canonical visibility.
    /// </summary>
    public static ConversationVisibility ToConversationVisibility(this bool isPrivate)
    {
        return isPrivate
            ? ConversationVisibility.Private
            : ConversationVisibility.Public;
    }

    /// <summary>
    /// Converts project ownership to canonical scope.
    /// </summary>
    public static ConversationScope ToConversationScope(this bool hasProjectId)
    {
        return hasProjectId
            ? ConversationScope.ProjectScoped
            : ConversationScope.TenantWide;
    }
}
