using Shuttle.Core.Contract;
using Shuttle.Core.Pipelines;

namespace Shuttle.Hopper;

public class HandlerContext<T>(IMessageSender messageSender, IState state, T message) : IHandlerContext<T> where T : class
{
    private readonly IMessageSender _messageSender = Guard.AgainstNull(messageSender);

    public T Message { get; } = Guard.AgainstNull(message);

    public IState State { get; } = Guard.AgainstNull(state);

    public async Task<TransportMessage> SendAsync(object message, Action<TransportMessageBuilder>? builder = null, CancellationToken cancellationToken1 = default)
    {
        return await _messageSender.SendAsync(message, builder, cancellationToken1).ConfigureAwait(false);
    }

    public async Task<IEnumerable<TransportMessage>> PublishAsync(object message, Action<TransportMessageBuilder>? builder = null, CancellationToken cancellationToken1 = default)
    {
        return await _messageSender.PublishAsync(message, builder, cancellationToken1).ConfigureAwait(false);
    }
}