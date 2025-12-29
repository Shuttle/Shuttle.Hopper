namespace Shuttle.Hopper;

public class DeferredMessageProcessingHaltedEventArgs(DateTimeOffset restartDateTime) : EventArgs
{
    public DateTimeOffset RestartDateTime { get; } = restartDateTime;
}