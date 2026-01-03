namespace Shuttle.Hopper;

public class DeferredMessageProcessingHaltedEventArgs(DateTimeOffset restartAt)
{
    public DateTimeOffset RestartAt { get; } = restartAt;
}