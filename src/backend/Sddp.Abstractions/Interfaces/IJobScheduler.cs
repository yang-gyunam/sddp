namespace Sddp.Abstractions.Interfaces;

/// <summary>
///
/// Application
/// </summary>
public interface IJobScheduler
{
    /// <summary>
    /// (fire-and-forget)
    /// </summary>
    /// <typeparam name="TJob">Quartz IJob </typeparam>
    /// <param name="data"> (JobDataMap)</param>
    /// <returns>create </returns>
    Task<string> EnqueueAsync<TJob>(IDictionary<string, object> data) where TJob : class;
}
