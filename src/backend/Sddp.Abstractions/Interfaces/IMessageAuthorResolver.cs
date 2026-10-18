using Sddp.Abstractions.DTOs;

namespace Sddp.Abstractions.Interfaces;

/// <summary>Resolves display metadata only for message IDs already authorized by the caller.</summary>
public interface IMessageAuthorResolver
{
    Task<IReadOnlyDictionary<Guid, MessageAuthorDto>> ResolveAsync(
        Guid tenant, Guid? project, Guid conversation, IReadOnlyCollection<Guid> messageIds, CancellationToken ct);
}
