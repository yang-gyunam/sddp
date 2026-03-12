namespace Sddp.Abstractions.Interfaces;

/// <summary>
/// AI
/// Application
/// </summary>
public interface IAnalysisTriggerService
{
    /// <summary>
    /// ()
    /// (analysisType + targetId) Pending/Processing
    /// 5
    /// </summary>
    /// <param name="tenantId">tenant ID</param>
    /// <param name="projectId">project ID</param>
    /// <param name="analysisType"> type (Reminder, Summary, MissingField, Conflict, Quality, Impact)</param>
    /// <param name="targetId"> ID</param>
    /// <param name="targetType"> type (spec, conversation)</param>
    /// <param name="inputText"> ()</param>
    /// <param name="cancellationToken"> </param>
    /// <returns>create reportId (null)</returns>
    Task<Guid?> TriggerAsync(
        Guid tenantId,
        Guid projectId,
        string analysisType,
        Guid targetId,
        string targetType,
        string? inputText = null,
        CancellationToken cancellationToken = default);
}
