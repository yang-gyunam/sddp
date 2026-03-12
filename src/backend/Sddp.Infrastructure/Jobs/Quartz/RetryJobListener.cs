using Microsoft.Extensions.Logging;
using Quartz;

namespace Sddp.Infrastructure.Jobs.Quartz;

/// <summary>
/// Quartz IJobListener —
/// [AutomaticRetry]. JobDataMap retry.
/// </summary>
public class RetryJobListener : IJobListener
{
    public const string RetryMaxKey = "retry:max";
    public const string RetryCountKey = "retry:count";
    public const string RetryDelaysKey = "retry:delays";

    private readonly ILogger<RetryJobListener> _logger;

    public string Name => "RetryJobListener";

    public RetryJobListener(ILogger<RetryJobListener> logger)
    {
        _logger = logger;
    }

    public Task JobToBeExecuted(IJobExecutionContext context, CancellationToken cancellationToken = default)
        => Task.CompletedTask;

    public Task JobExecutionVetoed(IJobExecutionContext context, CancellationToken cancellationToken = default)
        => Task.CompletedTask;

    public async Task JobWasExecuted(
        IJobExecutionContext context,
        JobExecutionException? jobException,
        CancellationToken cancellationToken = default)
    {
        if (jobException is null)
            return;

        var dataMap = context.JobDetail.JobDataMap;
        var maxRetries = dataMap.ContainsKey(RetryMaxKey) ? dataMap.GetInt(RetryMaxKey) : 0;
        var currentRetry = dataMap.ContainsKey(RetryCountKey) ? dataMap.GetInt(RetryCountKey) : 0;

        if (currentRetry >= maxRetries)
        {
            _logger.LogError(
                jobException,
                "Job {JobKey} failed after {RetryCount}/{MaxRetries} retries. Giving up.",
                context.JobDetail.Key, currentRetry, maxRetries);
            return;
        }

        //
        var delay = GetRetryDelay(dataMap, currentRetry);

        _logger.LogWarning(
            jobException,
            "Job {JobKey} failed (attempt {RetryCount}/{MaxRetries}). Retrying in {Delay}s.",
            context.JobDetail.Key, currentRetry + 1, maxRetries, delay.TotalSeconds);

        //
        var newDataMap = new JobDataMap((IDictionary<string, object>)context.JobDetail.JobDataMap);
        newDataMap.Put(RetryCountKey, (currentRetry + 1).ToString());

        var retryJob = JobBuilder.Create(context.JobDetail.JobType)
            .WithIdentity($"{context.JobDetail.Key.Name}-retry{currentRetry + 1}")
            .UsingJobData(newDataMap)
            .StoreDurably(false)
            .Build();

        var retryTrigger = TriggerBuilder.Create()
            .ForJob(retryJob.Key)
            .StartAt(DateTimeOffset.UtcNow.Add(delay))
            .Build();

        await (context.Scheduler.ScheduleJob(retryJob, retryTrigger, cancellationToken)).ConfigureAwait(false);
    }

    private static TimeSpan GetRetryDelay(JobDataMap dataMap, int currentRetry)
    {
        if (dataMap.ContainsKey(RetryDelaysKey))
        {
            var delaysStr = dataMap.GetString(RetryDelaysKey) ?? "";
            var delays = delaysStr.Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(s => int.TryParse(s.Trim(), out var v) ? v : 30)
                .ToArray();

            var index = Math.Min(currentRetry, delays.Length - 1);
            return TimeSpan.FromSeconds(delays[index]);
        }

        // default: (30, 60, 120,...)
        return TimeSpan.FromSeconds(30 * Math.Pow(2, currentRetry));
    }
}
