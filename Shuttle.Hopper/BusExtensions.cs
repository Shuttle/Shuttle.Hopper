using Shuttle.Core.Contract;

namespace Shuttle.Hopper;

public static class BusExtensions
{
    extension(IBus bus)
    {
        public async Task<IEnumerable<TransportMessage>> PublishAsync(object message, CancellationToken cancellationToken = default)
        {
            return await Guard.AgainstNull(bus).PublishAsync(message, null, cancellationToken);
        }

        public async Task<TransportMessage> SendAsync(object message, CancellationToken cancellationToken = default)
        {
            return await Guard.AgainstNull(bus).SendAsync(message, null, cancellationToken);
        }
    }
}