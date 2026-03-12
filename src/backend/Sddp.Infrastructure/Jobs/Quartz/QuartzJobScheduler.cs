using Quartz;
using Sddp.Abstractions.Interfaces;

namespace Sddp.Infrastructure.Jobs.Quartz;

/// <summary>
/// Quartz.NET IJobScheduler
/// fire-and-forget
/// </summary>
public class QuartzJobScheduler : IJobScheduler
{
    private readonly ISchedulerFactory _schedulerFactory;

    public QuartzJobScheduler(ISchedulerFactory schedulerFactory)
    {
        _schedulerFactory = schedulerFactory;
    }

    public async Task<string> EnqueueAsync<TJob>(IDictionary<string, object> data) where TJob : class
    {
        var scheduler = await (_schedulerFactory.GetScheduler()).ConfigureAwait(false);

        var jobKey = new JobKey($"{typeof(TJob).Name}-{Guid.NewGuid():N}");

        var jobDataMap = new JobDataMap();
        foreach (var kvp in data)
        {
            jobDataMap.Put(kvp.Key, kvp.Value?.ToString() ?? string.Empty);
        }

        var job = JobBuilder.Create(typeof(TJob))
            .WithIdentity(jobKey)
            .UsingJobData(jobDataMap)
            .StoreDurably(false)
            .Build();

        var trigger = TriggerBuilder.Create()
            .ForJob(jobKey)
            .StartNow()
            .Build();

        await (scheduler.ScheduleJob(job, trigger)).ConfigureAwait(false);

        return jobKey.Name;
    }
}
