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

    public Task SendAsync(Stream stream, IState state, CancellationToken cancellationToken = default)
    {
        return _transport.SendAsync(stream, Guard.AgainstNull(state), cancellationToken);
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