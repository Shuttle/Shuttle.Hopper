namespace Shuttle.Hopper;

public class DeferredMessageProcessingHaltedEventArgs : EventArgs
{
    public DeferredMessageProcessingHaltedEventArgs(DateTime restartDateTime)
    {
        RestartDateTime = restartDateTime;
    }

    public DateTime RestartDateTime { get; }
}