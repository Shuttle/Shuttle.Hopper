namespace Shuttle.Hopper;

public interface IMessageSender
{
    Task DispatchAsync(TransportMessage transportMessage, TransportMessage? transportMessageReceived = null, CancellationToken cancellationToken = default);
    Task<IEnumerable<TransportMessage>> PublishAsync(object message, TransportMessage? transportMessageReceived = null, Action<TransportMessageBuilder>? builder = null, CancellationToken cancellationToken = default);
    Task<TransportMessage> SendAsync(object message, TransportMessage? transportMessageReceived = null, Action<TransportMessageBuilder>? builder = null, CancellationToken cancellationToken = default);
}