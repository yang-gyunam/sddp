namespace Sddp.Abstractions.Enums;

/// <summary>
/// DirectMessage status (direct_messages)
/// status: Active → Concluded → Archived
/// </summary>
public enum DirectMessageStatus
{
    /// <summary> - conversation progress </summary>
    Active = 0,

    /// <summary> - conversation </summary>
    Concluded = 1,

    /// <summary> - </summary>
    Archived = 2
}

/// <summary>
/// DirectMessageStatus
/// </summary>
public static class DirectMessageStatusExtensions
{
    /// <summary>message status </summary>
    public static bool CanAddMessage(this DirectMessageStatus status)
        => status == DirectMessageStatus.Active;

    /// <summary> status </summary>
    public static bool CanConclude(this DirectMessageStatus status)
        => status == DirectMessageStatus.Active;

    /// <summary> status </summary>
    public static bool CanReopen(this DirectMessageStatus status)
        => status == DirectMessageStatus.Concluded;

    /// <summary> status </summary>
    public static bool CanArchive(this DirectMessageStatus status)
        => status == DirectMessageStatus.Concluded;
}
