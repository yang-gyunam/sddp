using Sddp.Abstractions.ValueObjects;

namespace Sddp.Application.Requests;

/// <summary>
/// Marks a command whose committed result changes whether an account may act.
/// </summary>
public interface IChangesActiveUserStatus
{
    GlobalUniqueId UserId { get; }

    bool IsActiveAfterCommit { get; }
}
