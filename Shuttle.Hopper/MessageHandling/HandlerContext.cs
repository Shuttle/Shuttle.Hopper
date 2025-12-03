using Shuttle.Core.Contract;

namespace Shuttle.Hopper;

public class HandlerContext<T>(IMessageSender messageSender, TransportMessage transportMessage, T message) : IHandlerContext<T> where T : class
{
    private readonly IMessageSender _messageSender = Guard.AgainstNull(messageSender);

    public TransportMessage TransportMessage { get; } = Guard.AgainstNull(transportMessage);
    public T Message { get; } = Guard.AgainstNull(message);
    public ExceptionHandling ExceptionHandling { get; set; } = ExceptionHandling.Default;

    public async Task<TransportMessage> SendAsync(object message, Action<TransportMessageBuilder>? builder = null, CancellationToken cancellationToken1 = default)
    {
        return await _messageSender.SendAsync(message, TransportMessage, builder, cancellationToken1).ConfigureAwait(false);
    }

    public async Task<IEnumerable<TransportMessage>> PublishAsync(object message, Action<TransportMessageBuilder>? builder = null, CancellationToken cancellationToken1 = default)
    {
        return await _messageSender.PublishAsync(message, TransportMessage, builder, cancellationToken1).ConfigureAwait(false);
    }
}