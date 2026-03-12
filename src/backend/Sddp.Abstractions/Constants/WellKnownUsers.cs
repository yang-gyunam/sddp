namespace Sddp.Abstractions.Constants;

/// <summary>
/// Well-known system user IDs.
/// UUID pattern: 00000000-0000-0000-0005-0000000000XX
/// </summary>
public static class WellKnownUsers
{
    public static readonly Guid AdminUserId = Guid.Parse("00000000-0000-0000-0005-000000000001");
    public static readonly Guid AiAgentUserId = Guid.Parse("00000000-0000-0000-0005-000000000099");
}
