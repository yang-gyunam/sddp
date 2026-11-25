namespace Sddp.Api.Security;

internal sealed class SensitiveReadOptions
{
    internal const string Section = "Security:SensitiveReads";

    public bool Enabled { get; set; } = true;
    public long FiveMinuteBytes { get; set; } = 32 * 1024 * 1024;
    public int FiveMinuteRequests { get; set; } = 250;
    public long HourlyBytes { get; set; } = 128 * 1024 * 1024;
    public int HourlyRequests { get; set; } = 1000;
    public long DailyBytes { get; set; } = 512 * 1024 * 1024;
    public int DailyRequests { get; set; } = 4000;

    internal ReadWindow[] Windows() =>
    [
        new(300, FiveMinuteBytes, FiveMinuteRequests),
        new(3600, HourlyBytes, HourlyRequests),
        new(86400, DailyBytes, DailyRequests)
    ];
}

internal readonly record struct ReadWindow(int Seconds, long ByteLimit, int RequestLimit);
