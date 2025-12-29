namespace Shuttle.Hopper;

public class DeferredMessageProcessingAdjustedEventArgs(DateTimeOffset nextProcessingDateTime) : EventArgs
{
    public DateTimeOffset NextProcessingDateTime { get; } = nextProcessingDateTime;
}