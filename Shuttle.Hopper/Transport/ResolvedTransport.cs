using Shuttle.Core.Contract;

namespace Shuttle.Hopper;

public class ResolvedTransport : ITransport
{
    private readonly ITransport _transport;

    public ResolvedTransport(ServiceBusOptions serviceBusOptions, ITransport transport, Uri uri)
    {
        _transport = Guard.AgainstNull(transport);
        Uri = new(Guard.AgainstNull(uri));
        Type = _transport.Type;
    }

    public TransportType Type { get; }
    public TransportUri Uri { get; }

    public async Task SendAsync(TransportMessage transportMessage, Stream stream, CancellationToken cancellationToken = default)
    {
        await _transport.SendAsync(transportMessage, stream, cancellationToken).ConfigureAwait(false);
    }

    public async Task<ReceivedMessage?> ReceiveAsync(CancellationToken cancellationToken = default)
    {
        return await _transport.ReceiveAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task AcknowledgeAsync(object acknowledgementToken, CancellationToken cancellationToken = default)
    {
        await _transport.AcknowledgeAsync(acknowledgementToken, cancellationToken).ConfigureAwait(false);
    }

    public async Task ReleaseAsync(object acknowledgementToken, CancellationToken cancellationToken = default)
    {
        await _transport.ReleaseAsync(acknowledgementToken, cancellationToken).ConfigureAwait(false);
    }
}