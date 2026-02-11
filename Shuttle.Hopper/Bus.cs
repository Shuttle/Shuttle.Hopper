using Shuttle.Core.Contract;

namespace Shuttle.Hopper;

public class Bus(IBusControl busControl, IMessageSender messageSender) : IBus
{
    public async Task<TransportMessage> SendAsync(object message, Action<TransportMessageBuilder>? builder = null, CancellationToken cancellationToken = default)
    {
        StartedGuard();

        return await Guard.AgainstNull(messageSender).SendAsync(message, builder, cancellationToken).ConfigureAwait(false);
    }

    public async Task<IEnumerable<TransportMessage>> PublishAsync(object message, Action<TransportMessageBuilder>? builder = null, CancellationToken cancellationToken = default)
    {
        StartedGuard();

        return await Guard.AgainstNull(messageSender).PublishAsync(message, builder, cancellationToken);
    }

    private void StartedGuard()
    {
        if (Guard.AgainstNull(busControl).Started)
        {
            return;
        }

        throw new InvalidOperationException(Resources.BusInstanceNotStarted);
    }
}