using Shuttle.Core.Pipelines;

namespace Shuttle.Hopper;

public interface ITransport
{
    TransportType Type { get; }
    TransportUri Uri { get; }
    ValueTask<bool> HasPendingAsync(CancellationToken cancellationToken = default);
    Task AcknowledgeAsync(object acknowledgementToken, CancellationToken cancellationToken = default);
    Task<ReceivedMessage?> ReceiveAsync(CancellationToken cancellationToken = default);
    Task ReleaseAsync(object acknowledgementToken, CancellationToken cancellationToken = default);
    Task SendAsync(Stream stream, IState state, CancellationToken cancellationToken = default);
}