using MediatR;
using Sddp.Abstractions.Interfaces;
using Sddp.Application.Requests;
using Sddp.Application.Telemetry;

namespace Sddp.Application.Behaviors;

public sealed class TransactionBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly IUnitOfWork _unitOfWork;

    public TransactionBehavior(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        using var activity = MediatRActivitySource.Source.StartActivity(
            $"Transaction {typeof(TRequest).Name}");

        var isCommand = request is ICommand<TResponse>;
        var ownsPersistenceBoundary = request is INonTransactionalCommand;
        activity?.SetTag("transaction.is_command", isCommand);
        activity?.SetTag("transaction.owns_persistence_boundary", ownsPersistenceBoundary);

        if (!isCommand || ownsPersistenceBoundary)
        {
            return await (next()).ConfigureAwait(false);
        }

        return await (_unitOfWork.ExecuteInTransactionAsync(
            _ => next(),
            cancellationToken)).ConfigureAwait(false);
    }
}
