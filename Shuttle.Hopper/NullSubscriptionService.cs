namespace Shuttle.Hopper;

public class NullSubscriptionService : ISubscriptionService
{
    public Task<IEnumerable<string>> GetSubscribedUrisAsync(string messageType, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException("NullSubscriptionManager");
    }
}