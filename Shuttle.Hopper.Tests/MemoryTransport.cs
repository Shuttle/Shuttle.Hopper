using Shuttle.Contract;
using Shuttle.Pipelines;
using Shuttle.Streams;

namespace Shuttle.Hopper.Tests;

public class MemoryTransport(HopperOptions hopperOptions, Uri uri) : ITransport
{
    private readonly SemaphoreSlim _lock = new(1, 1);
    private readonly Queue<Message> _queue = new();
    private readonly HopperOptions _hopperOptions = Guard.AgainstNull(hopperOptions);
    private readonly Dictionary<Guid, Message> _unacknowledged = new();

    public TransportType Type { get; } = TransportType.Queue;
    public TransportUri Uri { get; } = new(Guard.AgainstNull(uri));

    public async Task SendAsync(Stream stream, IPipeline pipeline, CancellationToken cancellationToken = default)
    {
        var copy = await Guard.AgainstNull(stream).CopyAsync(cancellationToken).ConfigureAwait(false);
        var transportMessage = Guard.AgainstNull(pipeline.State.GetTransportMessage());

        await _lock.WaitAsync(cancellationToken);

        try
        {
            _queue.Enqueue(new(transportMessage, copy));
        }
        finally
        {
            _lock.Release();
        }

        await _hopperOptions.MessageSent.InvokeAsync(new(this, copy, pipeline), cancellationToken).ConfigureAwait(false);
    }

    public async Task<ReceivedMessage?> ReceiveAsync(IPipeline pipeline, CancellationToken cancellationToken = default)
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

        await _hopperOptions.MessageReceived.InvokeAsync(new(this, result, pipeline), cancellationToken).ConfigureAwait(false);

        return result;
    }

    public ValueTask<bool> HasPendingAsync(CancellationToken cancellationToken = default)
    {
        return new(_queue.Count > 0);
    }

    public async Task AcknowledgeAsync(object acknowledgementToken, IPipeline pipeline, CancellationToken cancellationToken = default)
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

        await _hopperOptions.MessageAcknowledged.InvokeAsync(new(this, acknowledgementToken, pipeline), cancellationToken).ConfigureAwait(false);
    }

    public async Task ReleaseAsync(object acknowledgementToken, IPipeline pipeline, CancellationToken cancellationToken = default)
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

        await _hopperOptions.MessageReleased.InvokeAsync(new(this, acknowledgementToken, pipeline), cancellationToken);
    }

    private class Message(TransportMessage transportMessage, Stream stream)
    {
        public Stream Stream { get; } = stream;
        public TransportMessage TransportMessage { get; } = transportMessage;
    }
}