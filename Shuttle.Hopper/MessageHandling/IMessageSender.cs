namespace Shuttle.Hopper;

public interface IMessageSender
{
    Task DispatchAsync(TransportMessage transportMessage, CancellationToken cancellationToken = default);
    Task<IEnumerable<TransportMessage>> PublishAsync(object message, Action<TransportMessageBuilder>? builder = null, CancellationToken cancellationToken = default);
    Task<TransportMessage> SendAsync(object message, Action<TransportMessageBuilder>? builder = null, CancellationToken cancellationToken = default);
}