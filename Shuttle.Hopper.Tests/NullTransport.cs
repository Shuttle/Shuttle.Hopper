using Shuttle.Core.Contract;

namespace Shuttle.Hopper.Tests;

public class NullTransport(ServiceBusOptions serviceBusOptions, Uri uri) : ITransport
{
    private readonly ServiceBusOptions _serviceBusOptions = Guard.AgainstNull(serviceBusOptions);

    public TransportType Type { get; } = TransportType.Queue;
    public TransportUri Uri { get; } = new(uri);

    public async Task SendAsync(TransportMessage transportMessage, Stream stream, CancellationToken cancellationToken = default)
    {
        await _serviceBusOptions.MessageSent.InvokeAsync(new(this, transportMessage, stream), cancellationToken).ConfigureAwait(false);
    }

    public async Task<ReceivedMessage?> ReceiveAsync(CancellationToken cancellationToken = default)
    {
        await _serviceBusOptions.MessageReceived.InvokeAsync(new(this, new(Stream.Null, "token")), cancellationToken).ConfigureAwait(false);

        return null;
    }

    public async Task AcknowledgeAsync(object acknowledgementToken, CancellationToken cancellationToken = default)
    {
        await _serviceBusOptions.MessageAcknowledged.InvokeAsync(new(this, acknowledgementToken), cancellationToken).ConfigureAwait(false);
    }

    public async Task ReleaseAsync(object acknowledgementToken, CancellationToken cancellationToken = default)
    {
        await _serviceBusOptions.MessageReleased.InvokeAsync(new(this, acknowledgementToken), cancellationToken).ConfigureAwait(false);
    }
}