namespace Shuttle.Hopper;

public enum SubscriptionMode
{
    Standard = 0,
    FailWhenMissing = 1,
    Disabled = 2
}

public class SubscriptionOptions
{
    public List<string> MessageTypes { get; set; } = [];
    public SubscriptionMode Mode { get; set; } = SubscriptionMode.Standard;
    public TimeSpan? CacheTimeout { get; set; }
}