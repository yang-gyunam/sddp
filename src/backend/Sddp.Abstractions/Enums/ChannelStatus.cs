namespace Sddp.Abstractions.Enums;

/// <summary>
/// Channel status (channels)
/// DB codes: CHANNEL_STATUS/ACTIVE, CONCLUDED, ARCHIVED
/// </summary>
public enum ChannelStatus
{
    /// <summary> - progress </summary>
    Active = 0,

    /// <summary> - </summary>
    Concluded = 1,

    /// <summary> - </summary>
    Archived = 2
}

/// <summary>
/// ChannelStatus
/// </summary>
public static class ChannelStatusExtensions
{
    /// <summary>message status </summary>
    public static bool CanAddMessage(this ChannelStatus status)
        => status == ChannelStatus.Active;

    /// <summary> status </summary>
    public static bool CanConclude(this ChannelStatus status)
        => status == ChannelStatus.Active;

    /// <summary> status </summary>
    public static bool CanReopen(this ChannelStatus status)
        => status == ChannelStatus.Concluded;

    /// <summary> status </summary>
    public static bool CanArchive(this ChannelStatus status)
        => status == ChannelStatus.Concluded;
}
