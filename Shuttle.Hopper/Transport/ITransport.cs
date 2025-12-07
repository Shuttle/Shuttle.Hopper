namespace Shuttle.Hopper;

public interface ITransport
{
    TransportType Type { get; }
    TransportUri Uri { get; }
    ValueTask<bool> HasPendingAsync(CancellationToken cancellationToken = default);
    Task AcknowledgeAsync(object acknowledgementToken, CancellationToken cancellationToken = default);
    Task<ReceivedMessage?> ReceiveAsync(CancellationToken cancellationToken = default);
    Task ReleaseAsync(object acknowledgementToken, CancellationToken cancellationToken = default);
    Task SendAsync(TransportMessage transportMessage, Stream stream, CancellationToken cancellationToken = default);
}