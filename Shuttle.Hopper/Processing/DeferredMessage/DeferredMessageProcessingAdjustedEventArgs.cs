namespace Shuttle.Hopper;

public class DeferredMessageProcessingAdjustedEventArgs(DateTimeOffset nextProcessingAt) 
{
    public DateTimeOffset NextProcessingAt { get; } = nextProcessingAt;
}