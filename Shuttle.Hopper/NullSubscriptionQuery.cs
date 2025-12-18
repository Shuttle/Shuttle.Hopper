namespace Shuttle.Hopper;

public class NullSubscriptionQuery : ISubscriptionQuery
{
    public Task<IEnumerable<string>> GetSubscribedUrisAsync(string messageType, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException("NullSubscriptionQuery");
    }
}