using Sddp.Abstractions.ValueObjects;

namespace Sddp.Abstractions.Security;

/// <summary>
/// Shared key contract for process-local and distributed account-status state.
/// </summary>
public static class ActiveUserStatusKeys
{
    public static readonly TimeSpan LocalCacheDuration = TimeSpan.FromSeconds(30);

    public static string Local(GlobalUniqueId userId) => $"user-active:{userId}";

    public static string DistributedDeactivation(GlobalUniqueId userId) =>
        $"user-deactivated:{userId}";
}
