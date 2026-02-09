using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Shuttle.Core.Contract;

namespace Shuttle.Hopper;

public class SubscriptionService(IOptions<HopperOptions> hopperOptions, ISubscriptionQuery subscriptionQuery) : ISubscriptionService
{
    private static readonly SemaphoreSlim Lock = new(1, 1);
    private readonly IMemoryCache _subscribersCache = new MemoryCache(new MemoryCacheOptions());

    private readonly HopperOptions _hopperOptions = Guard.AgainstNull(Guard.AgainstNull(hopperOptions).Value);
    private readonly ISubscriptionQuery _subscriptionQuery = Guard.AgainstNull(subscriptionQuery);

    public async Task<IEnumerable<string>> GetSubscribedUrisAsync(string messageType, CancellationToken cancellationToken = default)
    {
        Guard.AgainstEmpty(messageType);

        await Lock.WaitAsync(CancellationToken.None).ConfigureAwait(false);

        try
        {
            if (_hopperOptions.Subscription.CacheTimeout != null &&
                _subscribersCache.TryGetValue(messageType, out IEnumerable<string>? subscribers) &&
                subscribers != null)
            {
                return subscribers;
            }

            subscribers = await _subscriptionQuery.GetSubscribedUrisAsync(messageType, cancellationToken);

            if (_hopperOptions.Subscription.CacheTimeout != null)
            {
                var cacheEntryOptions = new MemoryCacheEntryOptions()
                    .SetAbsoluteExpiration(_hopperOptions.Subscription.CacheTimeout.Value);

                _subscribersCache.Set(messageType, subscribers, cacheEntryOptions);
            }

            return subscribers;
        }
        finally
        {
            Lock.Release();
        }
    }
}