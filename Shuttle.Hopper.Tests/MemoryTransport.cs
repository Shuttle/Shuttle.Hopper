using Shuttle.Core.Contract;
using Shuttle.Core.Streams;

namespace Shuttle.Hopper.Tests;

public class MemoryTransport(ServiceBusOptions serviceBusOptions, Uri uri) : ITransport
{
    private readonly SemaphoreSlim _lock = new(1, 1);
    private readonly Queue<Message> _queue = new();
    private readonly ServiceBusOptions _serviceBusOptions = Guard.AgainstNull(serviceBusOptions);
    private readonly Dictionary<Guid, Message> _unacknowledged = new();

    public TransportType Type { get; } = TransportType.Queue;
    public TransportUri Uri { get; } = new(Guard.AgainstNull(uri));

    public async Task SendAsync(TransportMessage transportMessage, Stream stream, CancellationToken cancellationToken = default)
    {
        var copy = await stream.CopyAsync().ConfigureAwait(false);

        await _lock.WaitAsync(cancellationToken);

        try
        {
            _queue.Enqueue(new(transportMessage, copy));
        }
        finally
        {
            _lock.Release();
        }

        await _serviceBusOptions.MessageSent.InvokeAsync(new(this, transportMessage, copy), cancellationToken).ConfigureAwait(false);
    }

    public async Task<ReceivedMessage?> ReceiveAsync(CancellationToken cancellationToken = default)
    {
        Message message;

        await _lock.WaitAsync(cancellationToken);

        try
        {
            if (_queue.Count == 0)
            {
                return null;
            }

            message = _queue.Dequeue();

            _unacknowledged.Add(message.TransportMessage.MessageId, message);
        }
        finally
        {
            _lock.Release();
        }

        var result = new ReceivedMessage(message.Stream, message.TransportMessage.MessageId);

        await _serviceBusOptions.MessageReceived.InvokeAsync(new(this, result), cancellationToken).ConfigureAwait(false);

        return result;
    }

    public ValueTask<bool> HasPendingAsync(CancellationToken cancellationToken = default)
    {
        return new(_queue.Count > 0);
    }

    public async Task AcknowledgeAsync(object acknowledgementToken, CancellationToken cancellationToken = default)
    {
        await _lock.WaitAsync(cancellationToken);

        try
        {
            _unacknowledged.Remove((Guid)acknowledgementToken);
        }
        finally
        {
            _lock.Release();
        }

        await _serviceBusOptions.MessageAcknowledged.InvokeAsync(new(this, acknowledgementToken), cancellationToken).ConfigureAwait(false);
    }

    public async Task ReleaseAsync(object acknowledgementToken, CancellationToken cancellationToken = default)
    {
        await _lock.WaitAsync(cancellationToken);

        try
        {
            var token = (Guid)acknowledgementToken;

            _queue.Enqueue(_unacknowledged[token]);
            _unacknowledged.Remove(token);
        }
        finally
        {
            _lock.Release();
        }

        await _serviceBusOptions.MessageReleased.InvokeAsync(new(this, acknowledgementToken), cancellationToken);
    }

    private class Message(TransportMessage transportMessage, Stream stream)
    {
        public Stream Stream { get; } = stream;
        public TransportMessage TransportMessage { get; } = transportMessage;
    }
}