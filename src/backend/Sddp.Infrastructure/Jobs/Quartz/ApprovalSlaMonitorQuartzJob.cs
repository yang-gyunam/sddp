using Microsoft.Extensions.Logging;
using Quartz;

namespace Sddp.Infrastructure.Jobs.Quartz;

/// <summary>
/// Quartz IJob → ApprovalSlaMonitorJob.ExecuteAsync()
/// 15 Recurring Job (cron)
/// </summary>
[DisallowConcurrentExecution]
public class ApprovalSlaMonitorQuartzJob : IJob
{
    private readonly ApprovalSlaMonitorJob _slaMonitorJob;
    private readonly ILogger<ApprovalSlaMonitorQuartzJob> _logger;

    public ApprovalSlaMonitorQuartzJob(
        ApprovalSlaMonitorJob slaMonitorJob,
        ILogger<ApprovalSlaMonitorQuartzJob> logger)
    {
        _slaMonitorJob = slaMonitorJob;
        _logger = logger;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        try
        {
            await (_slaMonitorJob.ExecuteAsync(context.CancellationToken)).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ApprovalSlaMonitorQuartzJob failed");
            throw new JobExecutionException(ex);
        }
    }
}
