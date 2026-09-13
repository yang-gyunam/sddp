using Sddp.Abstractions.ValueObjects;

namespace Sddp.Abstractions.Interfaces;

public interface IActiveUserGate
{
    /// <summary>
    /// True when the account behind <paramref name="userId"/> may act. A user that is not there is
    /// not active either — an id in a signed token that no longer resolves is a token to refuse,
    /// not one to trust.
    /// </summary>
    Task<bool> IsActiveAsync(GlobalUniqueId userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Records that <paramref name="userId"/> may no longer act, effective at once.
    ///
    /// Deliberately a write and not a removal. A removal placed before the commit leaves a window
    /// in which a concurrent request rereads the not-yet-committed record and caches "active" for
    /// a further cache lifetime, which is the outcome this exists to prevent. Writing the refusal
    /// closes that window, and if the commit does fail the caller is refused for one cache
    /// lifetime — the safe direction to be wrong in.
    /// </summary>
    void MarkDeactivated(GlobalUniqueId userId);

    /// <summary>
    /// Forgets whatever was remembered about <paramref name="userId"/>, so the next request reads
    /// the current record. Used when an account is reactivated, where being wrong for a cache
    /// lifetime would mean refusing someone who is allowed in.
    /// </summary>
    void Forget(GlobalUniqueId userId);
}
