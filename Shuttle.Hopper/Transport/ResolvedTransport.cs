using Shuttle.Contract;
using Shuttle.Pipelines;

namespace Shuttle.Hopper;

public class ResolvedTransport : ITransport
{
    private readonly ITransport _transport;

    public ResolvedTransport(ITransport transport, Uri uri)
    {
        _transport = Guard.AgainstNull(transport);
        Uri = new(Guard.AgainstNull(uri));
        Type = _transport.Type;
    }

    public TransportType Type { get; }
    public TransportUri Uri { get; }

    public Task SendAsync(Stream stream, IPipeline pipeline, CancellationToken cancellationToken = default)
    {
        return _transport.SendAsync(stream, Guard.AgainstNull(pipeline), cancellationToken);
    }

    public Task<ReceivedMessage?> ReceiveAsync(IPipeline pipeline, CancellationToken cancellationToken = default)
    {
        return _transport.ReceiveAsync(Guard.AgainstNull(pipeline), cancellationToken);
    }

    public ValueTask<bool> HasPendingAsync(CancellationToken cancellationToken = default)
    {
        return _transport.HasPendingAsync(cancellationToken);
    }

    public Task AcknowledgeAsync(object acknowledgementToken, IPipeline pipeline, CancellationToken cancellationToken = default)
    {
        return _transport.AcknowledgeAsync(acknowledgementToken, Guard.AgainstNull(pipeline), cancellationToken);
    }

    public Task ReleaseAsync(object acknowledgementToken, IPipeline pipeline, CancellationToken cancellationToken = default)
    {
        return _transport.ReleaseAsync(acknowledgementToken, Guard.AgainstNull(pipeline), cancellationToken);
    }
}