using Sddp.Abstractions.Interfaces;

namespace Sddp.Api.Services;

/// <summary>
/// Public runtime stub that disables background embedding triggers.
/// </summary>
public sealed class NoOpEmbeddingTriggerService : IEmbeddingTriggerService
{
    public void TriggerConversationEmbedding(Guid tenantId, Guid projectId, Guid conversationId)
    {
    }

    public void TriggerSpecEmbedding(Guid tenantId, Guid projectId, Guid specId)
    {
    }

    public void TriggerGlossaryTermEmbedding(Guid tenantId, Guid projectId, Guid termId)
    {
    }

    public void TriggerArtifactMappingEmbedding(Guid tenantId, Guid projectId, Guid mappingId)
    {
    }

    public void TriggerBulkEmbedding(Guid tenantId, Guid projectId)
    {
    }
}

/// <summary>
/// Public runtime stub that disables automatic analysis triggers.
/// </summary>
public sealed class NoOpAnalysisTriggerService : IAnalysisTriggerService
{
    public Task<Guid?> TriggerAsync(
        Guid tenantId,
        Guid projectId,
        string analysisType,
        Guid targetId,
        string targetType,
        string? inputText = null,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult<Guid?>(null);
    }
}
