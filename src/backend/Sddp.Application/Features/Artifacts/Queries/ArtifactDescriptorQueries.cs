using MediatR;
using Sddp.Abstractions.DTOs;
using Sddp.Abstractions.Interfaces;
using Sddp.Abstractions.ValueObjects;
using Sddp.Application.Requests;
using Sddp.Domain.Entities;

namespace Sddp.Application.Features.Artifacts.Queries;

/// <summary>
/// Descriptor for one artifact. Always projected from the database rather than read back
/// from the YAML file, so the two cannot drift and a missing file is not an error.
/// </summary>
public sealed record GetArtifactDescriptorQuery(
    GlobalUniqueId TenantId,
    GlobalUniqueId ProjectId,
    GlobalUniqueId ArtifactId) : IQuery<ArtifactDescriptorDto?>;

public sealed class GetArtifactDescriptorQueryHandler
    : IRequestHandler<GetArtifactDescriptorQuery, ArtifactDescriptorDto?>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IArtifactDescriptorSerializer _serializer;

    public GetArtifactDescriptorQueryHandler(
        IUnitOfWork unitOfWork,
        IArtifactDescriptorSerializer serializer)
    {
        _unitOfWork = unitOfWork;
        _serializer = serializer;
    }

    public async Task<ArtifactDescriptorDto?> Handle(
        GetArtifactDescriptorQuery request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var artifact = await (_unitOfWork.Repository<ArtifactTracking>()
            .GetByIdAsync(request.ArtifactId, cancellationToken)).ConfigureAwait(false);

        if (artifact == null
            || artifact.TenantId != request.TenantId
            || artifact.ProjectId != request.ProjectId
            || !artifact.IsActive)
        {
            return null;
        }

        var spec = await (_unitOfWork.Repository<Spec>()
            .GetByIdAsync(artifact.SpecId, cancellationToken)).ConfigureAwait(false);

        var descriptor = ArtifactDescriptorBuilder.Build(artifact, spec);

        return new ArtifactDescriptorDto(
            ArtifactId: artifact.Id.ToString(),
            SchemaVersion: descriptor.SchemaVersion,
            Yaml: _serializer.Serialize(descriptor));
    }
}

/// <summary>
/// The artifact file's actual contents, for review against the descriptor.
/// </summary>
public sealed record GetArtifactContentQuery(
    GlobalUniqueId TenantId,
    GlobalUniqueId ProjectId,
    GlobalUniqueId ArtifactId) : IQuery<ArtifactContentDto?>;

public sealed class GetArtifactContentQueryHandler
    : IRequestHandler<GetArtifactContentQuery, ArtifactContentDto?>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IArtifactRepositoryService _artifactRepositoryService;

    public GetArtifactContentQueryHandler(
        IUnitOfWork unitOfWork,
        IArtifactRepositoryService artifactRepositoryService)
    {
        _unitOfWork = unitOfWork;
        _artifactRepositoryService = artifactRepositoryService;
    }

    public async Task<ArtifactContentDto?> Handle(
        GetArtifactContentQuery request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var artifact = await (_unitOfWork.Repository<ArtifactTracking>()
            .GetByIdAsync(request.ArtifactId, cancellationToken)).ConfigureAwait(false);

        if (artifact == null
            || artifact.TenantId != request.TenantId
            || artifact.ProjectId != request.ProjectId
            || !artifact.IsActive)
        {
            return null;
        }

        var context = await (_artifactRepositoryService.GetProjectContextAsync(
            request.TenantId, request.ProjectId, cancellationToken)).ConfigureAwait(false);

        var file = await (_artifactRepositoryService.GetArtifactContentAsync(
            context, artifact.ArtifactPath, cancellationToken)).ConfigureAwait(false);

        if (file is null)
        {
            return null;
        }

        var status = await (ArtifactStatusHelper.DetermineStatusAsync(
            _artifactRepositoryService, context, artifact, cancellationToken)).ConfigureAwait(false);

        return new ArtifactContentDto(
            ArtifactId: artifact.Id.ToString(),
            Path: artifact.ArtifactPath,
            Content: file.Content,
            ContentHash: file.Hash,
            SizeBytes: file.SizeBytes,
            IsBinary: file.IsBinary,
            Status: status,
            Language: ArtifactLanguageResolver.FromPath(artifact.ArtifactPath),
            TooLarge: file.TooLarge,
            MaxBytes: file.MaxBytes);
    }
}
