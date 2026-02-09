namespace Shuttle.Hopper;

public interface IBus
{
    Task<IEnumerable<TransportMessage>> PublishAsync(object message, Action<TransportMessageBuilder>? builder = null, CancellationToken cancellationToken = default);
    Task<TransportMessage> SendAsync(object message, Action<TransportMessageBuilder>? builder = null, CancellationToken cancellationToken = default);
}