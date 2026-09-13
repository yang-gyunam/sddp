using Sddp.Abstractions.ValueObjects;

namespace Sddp.Application.Requests;

/// <summary>
/// Marks a command whose committed result should be mirrored to an artifact descriptor
/// file. Implementing this is the only wiring needed — <c>ArtifactDescriptorBehavior</c>
/// is registered as a constrained open generic, so a new command picks it up without a
/// registration anyone could forget.
/// </summary>
public interface IProducesArtifactDescriptor<in TResponse>
{
    GlobalUniqueId TenantId { get; }

    GlobalUniqueId ProjectId { get; }

    /// <summary>
    /// The artifact the command produced, or null when there is nothing to describe.
    /// </summary>
    GlobalUniqueId? GetDescribedArtifactId(TResponse response);
}
