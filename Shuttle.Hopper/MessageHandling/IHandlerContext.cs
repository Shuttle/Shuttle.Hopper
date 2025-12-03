namespace Shuttle.Hopper;

public enum ExceptionHandling
{
    Default = 0,
    Retry = 1,
    Block = 2,
    Poison = 3
}

public interface IHandlerContext
{
    CancellationToken CancellationToken { get; }
    ExceptionHandling ExceptionHandling { get; set; }
    TransportMessage TransportMessage { get; }
    Task<IEnumerable<TransportMessage>> PublishAsync(object message, Action<TransportMessageBuilder>? builder = null);
    Task<TransportMessage> SendAsync(object message, Action<TransportMessageBuilder>? builder = null);
}

public interface IHandlerContext<out T> : IHandlerContext where T : class
{
    T Message { get; }
}