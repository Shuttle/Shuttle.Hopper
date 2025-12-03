namespace Shuttle.Hopper;

public class ProcessorOptions
{
    public List<TimeSpan> DurationToIgnoreOnFailure { get; set; } = [];
    public List<TimeSpan> DurationToSleepWhenIdle { get; set; } = [];
    public string? ErrorTransportUri { get; set; }
    public int MaximumFailureCount { get; set; } = 5;
    public int ThreadCount { get; set; } = 1;
    public string? WorkTransportUri { get; set; }
}