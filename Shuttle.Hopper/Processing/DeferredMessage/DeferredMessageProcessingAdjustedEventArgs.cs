namespace Shuttle.Hopper;

public class DeferredMessageProcessingAdjustedEventArgs : EventArgs
{
    public DeferredMessageProcessingAdjustedEventArgs(DateTime nextProcessingDateTime)
    {
        NextProcessingDateTime = nextProcessingDateTime;
    }

    public DateTime NextProcessingDateTime { get; }
}