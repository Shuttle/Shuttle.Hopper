using Shuttle.Core.Pipelines;

namespace Shuttle.Hopper;

public interface IHandlerContext
{
    public IState State { get; }
    Task<IEnumerable<TransportMessage>> PublishAsync(object message, Action<TransportMessageBuilder>? builder = null, CancellationToken cancellationToken = default);
    Task<TransportMessage> SendAsync(object message, Action<TransportMessageBuilder>? builder = null, CancellationToken cancellationToken = default);
}

public interface IHandlerContext<out T> : IHandlerContext where T : class
{
    T Message { get; }
}