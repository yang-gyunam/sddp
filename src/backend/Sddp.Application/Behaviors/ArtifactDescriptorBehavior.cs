using MediatR;
using Microsoft.Extensions.Logging;
using Sddp.Abstractions.Interfaces;
using Sddp.Application.Features.Artifacts;
using Sddp.Application.Requests;
using Sddp.Domain.Entities;

namespace Sddp.Application.Behaviors;

/// <summary>
/// Materializes the artifact descriptor after the command's transaction commits.
///
/// Position matters: this is registered ahead of <see cref="TransactionBehavior{TRequest,TResponse}"/>,
/// so the work below <c>next()</c> runs post-commit. Writing the file inside the handler
/// would break the rule that handlers cause no external side effects before commit — the
/// execution strategy can re-run the delegate on retry, and a rolled-back command would
/// still have left a descriptor on disk.
/// </summary>
public sealed class ArtifactDescriptorBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IProducesArtifactDescriptor<TResponse>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IArtifactDescriptorStore _descriptorStore;
    private readonly ILogger<ArtifactDescriptorBehavior<TRequest, TResponse>> _logger;

    public ArtifactDescriptorBehavior(
        IUnitOfWork unitOfWork,
        IArtifactDescriptorStore descriptorStore,
        ILogger<ArtifactDescriptorBehavior<TRequest, TResponse>> logger)
    {
        _unitOfWork = unitOfWork;
        _descriptorStore = descriptorStore;
        _logger = logger;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(next);

        var response = await (next()).ConfigureAwait(false);

        var artifactId = request.GetDescribedArtifactId(response);
        if (artifactId is null)
        {
            return response;
        }

        try
        {
            var artifact = await (_unitOfWork.Repository<ArtifactTracking>()
                .GetByIdAsync(artifactId.Value, cancellationToken)).ConfigureAwait(false);

            if (artifact is null
                || artifact.TenantId != request.TenantId
                || artifact.ProjectId != request.ProjectId)
            {
                return response;
            }

            var spec = await (_unitOfWork.Repository<Spec>()
                .GetByIdAsync(artifact.SpecId, cancellationToken)).ConfigureAwait(false);

            var descriptor = ArtifactDescriptorBuilder.Build(artifact, spec);
            await (_descriptorStore.WriteAsync(
                request.ProjectId, descriptor, cancellationToken)).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            // The record is already committed and the descriptor is a regenerable
            // projection of it, so a write failure must not fail the command.
            _logger.LogError(
                ex,
                "Failed to write artifact descriptor for {ArtifactId} in project {ProjectId}",
                artifactId.Value,
                request.ProjectId);
        }

        return response;
    }
}
