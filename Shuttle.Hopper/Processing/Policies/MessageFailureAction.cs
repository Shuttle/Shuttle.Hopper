namespace Shuttle.Hopper;

public class MessageFailureAction(bool retry, TimeSpan timeSpanToIgnoreRetriedMessage)
{
    public bool Retry { get; } = retry;
    public TimeSpan TimeSpanToIgnoreRetriedMessage { get; } = timeSpanToIgnoreRetriedMessage;
}