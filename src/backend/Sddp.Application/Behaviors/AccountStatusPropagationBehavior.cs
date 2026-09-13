using MediatR;
using Sddp.Abstractions.Interfaces;
using Sddp.Application.Requests;

namespace Sddp.Application.Behaviors;

/// <summary>
/// Propagates an account status only after the inner transaction behavior has committed.
/// Registration order is part of the security contract: this behavior must wrap
/// <see cref="TransactionBehavior{TRequest,TResponse}"/>.
/// </summary>
public sealed class AccountStatusPropagationBehavior<TRequest, TResponse>
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IChangesActiveUserStatus
{
    private readonly IActiveUserStatusNotifier _notifier;

    public AccountStatusPropagationBehavior(IActiveUserStatusNotifier notifier)
    {
        _notifier = notifier;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(next);

        // The token belongs to the handler, which runs inside next(); nothing after it is
        // cancellable. See the comment below.
        _ = cancellationToken;

        var response = await next().ConfigureAwait(false);

        await _notifier.PublishAsync(
            request.UserId,
            request.IsActiveAfterCommit,
            CancellationToken.None).ConfigureAwait(false);

        return response;
    }
}
