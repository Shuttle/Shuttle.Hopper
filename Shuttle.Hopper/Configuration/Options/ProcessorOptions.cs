namespace Shuttle.Hopper;

public class ProcessorOptions
{
    public List<TimeSpan> IgnoreOnFailureDurations { get; set; } = [];
    public List<TimeSpan> IdleDurations { get; set; } = [];
    public Uri? ErrorTransportUri { get; set; }
    public int MaximumFailureCount { get; set; } = 5;
    public int ThreadCount { get; set; } = 1;
    public Uri? WorkTransportUri { get; set; }
}