using Shuttle.Core.Contract;

namespace Shuttle.Hopper;

public static class ServiceBusExtensions
{
    extension(IServiceBus serviceBus)
    {
        public async Task<IEnumerable<TransportMessage>> PublishAsync(object message, CancellationToken cancellationToken = default)
        {
            return await Guard.AgainstNull(serviceBus).PublishAsync(message, null, cancellationToken);
        }

        public async Task<TransportMessage> SendAsync(object message, CancellationToken cancellationToken = default)
        {
            return await Guard.AgainstNull(serviceBus).SendAsync(message, null, cancellationToken);
        }
    }
}