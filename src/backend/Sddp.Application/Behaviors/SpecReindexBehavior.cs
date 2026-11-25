using MediatR;
using Sddp.Abstractions.Events;
using Sddp.Abstractions.Interfaces;
using Sddp.Abstractions.ValueObjects;
using Sddp.Application.Requests;

namespace Sddp.Application.Behaviors;

/// <summary>
/// Records the Spec search reindex in the outbox, inside the command's transaction.
///
/// Registered after <see cref="TransactionBehavior{TRequest,TResponse}"/>, so the work below
/// <c>next()</c> runs inside the transaction once the handler has saved. The outbox row commits
/// with the Spec change and rolls back with it. Scheduling after the commit instead loses the
/// rebuild if the process stops or the scheduler fails in between, and the changed Spec stays out
/// of search. A command that throws records nothing.
/// </summary>
public sealed class SpecReindexBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IReindexesSpecs<TResponse>
{
    private readonly IOutboxEnqueuer _outbox;

    public SpecReindexBehavior(IOutboxEnqueuer outbox)
    {
        _outbox = outbox;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(next);

        var response = await (next()).ConfigureAwait(false);

        foreach (var specId in request.GetReindexedSpecIds(response).Distinct())
        {
            await (_outbox.EnqueueAsync(
                new SpecSearchReindexRequestedEvent(request.TenantId.ToGuid(), request.ProjectId.ToGuid(), specId),
                "spec",
                GlobalUniqueId.FromGuid(specId),
                cancellationToken)).ConfigureAwait(false);
        }

        return response;
    }
}
