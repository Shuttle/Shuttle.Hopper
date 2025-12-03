namespace Shuttle.Hopper;

public interface IServiceBus : IDisposable, IAsyncDisposable
{
    IInboxConfiguration? Inbox { get; }
    IOutboxConfiguration? Outbox { get; }
    bool Started { get; }
    Task<IEnumerable<TransportMessage>> PublishAsync(object message, Action<TransportMessageBuilder>? builder = null, CancellationToken cancellationToken = default);
    Task<TransportMessage> SendAsync(object message, Action<TransportMessageBuilder>? builder = null, CancellationToken cancellationToken = default);
    Task<IServiceBus> StartAsync(CancellationToken cancellationToken = default);
    Task StopAsync(CancellationToken cancellationToken = default);
}