namespace Shuttle.Hopper;

public interface ISubscriptionService
{
    Task<IEnumerable<string>> GetSubscribedUrisAsync(string messageType, CancellationToken cancellationToken = default);
}