using Shuttle.Core.Contract;

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

    public Task SendAsync(TransportMessage transportMessage, Stream stream, CancellationToken cancellationToken = default)
    {
        return _transport.SendAsync(transportMessage, stream, cancellationToken);
    }

    public Task<ReceivedMessage?> ReceiveAsync(CancellationToken cancellationToken = default)
    {
        return _transport.ReceiveAsync(cancellationToken);
    }

    public ValueTask<bool> HasPendingAsync(CancellationToken cancellationToken = default)
    {
        return _transport.HasPendingAsync(cancellationToken);
    }

    public Task AcknowledgeAsync(object acknowledgementToken, CancellationToken cancellationToken = default)
    {
        return _transport.AcknowledgeAsync(acknowledgementToken, cancellationToken);
    }

    public Task ReleaseAsync(object acknowledgementToken, CancellationToken cancellationToken = default)
    {
        return _transport.ReleaseAsync(acknowledgementToken, cancellationToken);
    }
}