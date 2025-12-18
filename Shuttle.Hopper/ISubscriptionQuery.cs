namespace Shuttle.Hopper;

public interface ISubscriptionQuery
{
    Task<IEnumerable<string>> GetSubscribedUrisAsync(string messageType, CancellationToken cancellationToken = default);
}