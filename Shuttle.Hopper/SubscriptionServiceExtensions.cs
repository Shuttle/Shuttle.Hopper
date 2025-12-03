using Shuttle.Core.Contract;

namespace Shuttle.Hopper;

public static class SubscriptionServiceExtensions
{
    extension(ISubscriptionService subscriptionService)
    {
        public async Task<IEnumerable<string>> GetSubscribedUrisAsync(object message, CancellationToken cancellationToken = default)
        {
            return await Guard.AgainstNull(subscriptionService).GetSubscribedUrisAsync(Guard.AgainstEmpty(Guard.AgainstNull(message).GetType().FullName), cancellationToken).ConfigureAwait(false);
        }
    }
}