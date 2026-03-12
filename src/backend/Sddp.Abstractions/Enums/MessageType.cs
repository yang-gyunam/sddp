namespace Sddp.Abstractions.Enums;

/// <summary>
/// Conversation message type
/// </summary>
public enum MessageType
{
    /// <summary>Proposal - new idea or change proposal</summary>
    Proposal = 0,

    /// <summary>Question - clarification request</summary>
    Question = 1,

    /// <summary>Objection - raised objection</summary>
    Objection = 2,

    /// <summary>Reference - sharing related material</summary>
    Reference = 3,

    /// <summary>Decision - final consensus item</summary>
    Decision = 4,

    /// <summary>AI Reminder - automatically generated notification by AI</summary>
    AiReminder = 5,

    /// <summary>AI Summary - automatically generated discussion summary by AI</summary>
    AiSummary = 6,

    /// <summary>AI Suggestion - automatically generated improvement/alternative suggestion by AI</summary>
    AiSuggestion = 7,

    /// <summary>Normal - plain message without formatting</summary>
    Normal = 8
}

/// <summary>
/// MessageType extension methods
/// </summary>
public static class MessageTypeExtensions
{
    /// <summary>
    /// Checks whether the message type is AI-generated
    /// </summary>
    public static bool IsAiGenerated(this MessageType type)
    {
        return type is MessageType.AiReminder or MessageType.AiSummary or MessageType.AiSuggestion;
    }
}
