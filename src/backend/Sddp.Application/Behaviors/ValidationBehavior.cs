using FluentValidation;
using MediatR;
using Sddp.Application.Telemetry;

namespace Sddp.Application.Behaviors;

public sealed class ValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly IEnumerable<IValidator<TRequest>> _validators;

    public ValidationBehavior(IEnumerable<IValidator<TRequest>> validators)
    {
        _validators = validators;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        using var activity = MediatRActivitySource.Source.StartActivity(
            $"Validate {typeof(TRequest).Name}");

        if (_validators.Any())
        {
            var context = new ValidationContext<TRequest>(request);
            var results = await (Task.WhenAll(
                _validators.Select(v => v.ValidateAsync(context, cancellationToken)))).ConfigureAwait(false);

            var failures = results
                .SelectMany(r => r.Errors)
                .Where(f => f is not null)
                .ToList();

            activity?.SetTag("validation.error_count", failures.Count);

            if (failures.Count > 0)
            {
                var errors = failures
                    .GroupBy(f => f.PropertyName)
                    .ToDictionary(
                        g => g.Key,
                        g => g.Select(f => f.ErrorMessage).ToArray());

                throw new Sddp.Abstractions.Exceptions.ValidationException(errors);
            }
        }

        return await (next()).ConfigureAwait(false);
    }
}
