using Shuttle.Pipelines;

namespace Shuttle.Hopper;

public interface ITransport
{
    TransportType Type { get; }
    TransportUri Uri { get; }
    ValueTask<bool> HasPendingAsync(CancellationToken cancellationToken = default);
    Task AcknowledgeAsync(object acknowledgementToken, IPipeline pipeline, CancellationToken cancellationToken = default);
    Task<ReceivedMessage?> ReceiveAsync(IPipeline pipeline, CancellationToken cancellationToken = default);
    Task ReleaseAsync(object acknowledgementToken, IPipeline pipeline, CancellationToken cancellationToken = default);
    Task SendAsync(Stream stream, IPipeline pipeline, CancellationToken cancellationToken = default);
}