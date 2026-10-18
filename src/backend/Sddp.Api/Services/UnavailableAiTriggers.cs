using Sddp.Abstractions.Interfaces;

namespace Sddp.Api.Services;

/// <summary>Core event callers remain usable without loading the optional module.</summary>
internal sealed class UnavailableAiTriggers : IEmbeddingTriggerService, IAnalysisTriggerService
{
    public void TriggerConversationEmbedding(Guid tenantId, Guid projectId, Guid conversationId) { }
    public void TriggerSpecEmbedding(Guid tenantId, Guid projectId, Guid specId) { }
    public void TriggerGlossaryTermEmbedding(Guid tenantId, Guid projectId, Guid termId) { }
    public void TriggerArtifactMappingEmbedding(Guid tenantId, Guid projectId, Guid mappingId) { }
    public void TriggerBulkEmbedding(Guid tenantId, Guid projectId) { }
    public Task<Guid?> TriggerAsync(Guid tenantId, Guid projectId, string analysisType, Guid targetId,
        string targetType, string? inputText = null, CancellationToken cancellationToken = default)
        => Task.FromResult<Guid?>(null);
}
