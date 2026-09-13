using Sddp.Abstractions.ValueObjects;

namespace Sddp.Abstractions.Interfaces;

/// <summary>
/// Propagates a committed account-status change to every API host.
/// </summary>
public interface IActiveUserStatusNotifier
{
    /// <summary>
    /// Publishes the authoritative status after the database transaction has committed.
    /// Completion means the distributed marker has been updated and the event accepted by the
    /// configured transport; failures must not be reported as a successful administration call.
    /// </summary>
    Task PublishAsync(
        GlobalUniqueId userId,
        bool isActive,
        CancellationToken cancellationToken = default);
}
