namespace Sddp.Abstractions.Enums;

/// <summary>
/// Conversation participant type
/// </summary>
public enum ParticipantType
{
    /// <summary>Human user</summary>
    Human = 0,

    /// <summary>AI Agent</summary>
    AI = 1
}

/// <summary>
/// ParticipantType extension methods
/// </summary>
public static class ParticipantTypeExtensions
{
    /// <summary>
    /// Checks whether AI-exclusive message types can be used
    /// </summary>
    public static bool CanUseAiMessageTypes(this ParticipantType type)
    {
        return type == ParticipantType.AI;
    }
}
