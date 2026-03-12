namespace Sddp.Abstractions.Interfaces;

/// <summary>
///
/// Message/Spec/Glossary Job create
/// </summary>
public interface IEmbeddingTriggerService
{
    /// <summary>
    /// Conversation all message window
    /// </summary>
    void TriggerConversationEmbedding(Guid tenantId, Guid projectId, Guid conversationId);

    /// <summary>
    /// Spec create/
    /// </summary>
    void TriggerSpecEmbedding(Guid tenantId, Guid projectId, Guid specId);

    /// <summary>
    /// Glossary glossary create/
    /// </summary>
    void TriggerGlossaryTermEmbedding(Guid tenantId, Guid projectId, Guid termId);

    /// <summary>
    /// Artifact-to-Spec create/
    /// </summary>
    void TriggerArtifactMappingEmbedding(Guid tenantId, Guid projectId, Guid mappingId);

    /// <summary>
    /// ()
    /// </summary>
    void TriggerBulkEmbedding(Guid tenantId, Guid projectId);
}
