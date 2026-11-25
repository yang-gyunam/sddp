using Sddp.Abstractions.ValueObjects;

namespace Sddp.Application.Requests;

/// <summary>
/// Marks a command after which the search index of the named Specs must be rebuilt.
///
/// Search uses a Spec chunk only while the Spec is current and its <c>updated_at</c> is not
/// newer than the chunk. Every Spec write moves that boundary, so without a rebuild the Spec
/// drops out of search. Implementing this is the only wiring needed: <c>SpecReindexBehavior</c>
/// is a constrained open generic that records the rebuild in the outbox in the same transaction.
/// </summary>
public interface IReindexesSpecs<in TResponse>
{
    GlobalUniqueId TenantId { get; }

    GlobalUniqueId ProjectId { get; }

    /// <summary>
    /// The Specs whose index must follow the committed result. Empty when nothing changed,
    /// for example when the Spec was not found.
    /// </summary>
    IReadOnlyList<Guid> GetReindexedSpecIds(TResponse response);
}
