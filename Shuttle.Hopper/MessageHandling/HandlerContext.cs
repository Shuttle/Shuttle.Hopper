using Shuttle.Core.Contract;

namespace Shuttle.Hopper;

public class HandlerContext<T>(IMessageSender messageSender, IMessageContext messageContext, TransportMessage transportMessage, T message) : IHandlerContext<T> where T : class
{
    private readonly IMessageSender _messageSender = Guard.AgainstNull(messageSender);
    private readonly IMessageContext _messageContext = Guard.AgainstNull(messageContext);

    public TransportMessage TransportMessage { get; } = Guard.AgainstNull(transportMessage);
    public T Message { get; } = Guard.AgainstNull(message);

    public ExceptionHandling ExceptionHandling
    {
        get => _messageContext.ExceptionHandling;
        set => _messageContext.ExceptionHandling = value;
    }

    public async Task<TransportMessage> SendAsync(object message, Action<TransportMessageBuilder>? builder = null, CancellationToken cancellationToken1 = default)
    {
        return await _messageSender.SendAsync(message, builder, cancellationToken1).ConfigureAwait(false);
    }

    public async Task<IEnumerable<TransportMessage>> PublishAsync(object message, Action<TransportMessageBuilder>? builder = null, CancellationToken cancellationToken1 = default)
    {
        return await _messageSender.PublishAsync(message, builder, cancellationToken1).ConfigureAwait(false);
    }
}