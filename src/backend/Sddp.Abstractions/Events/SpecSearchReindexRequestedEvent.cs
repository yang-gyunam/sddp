namespace Sddp.Abstractions.Events;

/// <summary>
/// The search index of one Spec must be rebuilt, recorded in the outbox in the transaction that
/// changed the Spec.
///
/// Search uses a Spec chunk only while the Spec is current and not updated after the chunk, so a
/// change that is committed without a rebuild drops the Spec out of search. Scheduling the rebuild
/// after the commit is not enough: the process can stop, or the scheduler can fail, in between.
/// The row survives both and is retried. A Spec that no longer exists clears its chunks.
/// </summary>
public sealed record SpecSearchReindexRequestedEvent(Guid TenantId, Guid ProjectId, Guid SpecId);
