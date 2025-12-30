using Shuttle.Core.Contract;

namespace Shuttle.Hopper.Tests;

public class NullTransport(HopperOptions hopperOptions, Uri uri) : ITransport
{
    private readonly HopperOptions _hopperOptions = Guard.AgainstNull(hopperOptions);

    public TransportType Type { get; } = TransportType.Queue;
    public TransportUri Uri { get; } = new(uri);

    public async Task SendAsync(TransportMessage transportMessage, Stream stream, CancellationToken cancellationToken = default)
    {
        await _hopperOptions.MessageSent.InvokeAsync(new(this, transportMessage, stream), cancellationToken).ConfigureAwait(false);
    }

    public async Task<ReceivedMessage?> ReceiveAsync(CancellationToken cancellationToken = default)
    {
        await _hopperOptions.MessageReceived.InvokeAsync(new(this, new(Stream.Null, "token")), cancellationToken).ConfigureAwait(false);

        return null;
    }

    public async ValueTask<bool> HasPendingAsync(CancellationToken cancellationToken = default)
    {
        await _hopperOptions.TransportOperation.InvokeAsync(new(this, "HasPendingAsync"), cancellationToken).ConfigureAwait(false);

        return false;
    }

    public async Task AcknowledgeAsync(object acknowledgementToken, CancellationToken cancellationToken = default)
    {
        await _hopperOptions.MessageAcknowledged.InvokeAsync(new(this, acknowledgementToken), cancellationToken).ConfigureAwait(false);
    }

    public async Task ReleaseAsync(object acknowledgementToken, CancellationToken cancellationToken = default)
    {
        await _hopperOptions.MessageReleased.InvokeAsync(new(this, acknowledgementToken), cancellationToken).ConfigureAwait(false);
    }
}