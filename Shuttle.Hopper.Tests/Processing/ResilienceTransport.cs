using System.Collections.Concurrent;
using Shuttle.Contract;
using Shuttle.Pipelines;
using Shuttle.Streams;

namespace Shuttle.Hopper.Tests;

/// <summary>
///     An in-memory transport that is safe to use from multiple processor threads concurrently.  Unlike
///     <see cref="MemoryTransport" /> the acknowledgement token is unique per receive, which allows the same
///     <see cref="TransportMessage.MessageId" /> to be in-flight and re-sent at the same time (which is exactly what
///     happens when a message is retried by the `ReceivePipelineFailedObserver`).
/// </summary>
public class ResilienceTransport(HopperOptions hopperOptions, Uri uri) : ITransport
{
    private readonly HopperOptions _hopperOptions = Guard.AgainstNull(hopperOptions);
    private readonly Lock _lock = new();
    private readonly Queue<Stream> _queue = new();
    private readonly ConcurrentDictionary<Guid, Stream> _unacknowledged = new();

    private int _receiveFaultCount;
    private int _sendCount;

    public int Count
    {
        get
        {
            lock (_lock)
            {
                return _queue.Count;
            }
        }
    }

    public int SendCount => Volatile.Read(ref _sendCount);
    public int UnacknowledgedCount => _unacknowledged.Count;

    public TransportType Type { get; } = TransportType.Queue;
    public TransportUri Uri { get; } = new(Guard.AgainstNull(uri));

    public async Task SendAsync(Stream stream, IPipeline pipeline, CancellationToken cancellationToken = default)
    {
        var copy = await Guard.AgainstNull(stream).CopyAsync(cancellationToken).ConfigureAwait(false);

        lock (_lock)
        {
            _queue.Enqueue(copy);
        }

        Interlocked.Increment(ref _sendCount);

        await _hopperOptions.MessageSent.InvokeAsync(new(this, copy, pipeline), cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    ///     Causes the next <paramref name="count" /> calls to <see cref="ReceiveAsync" /> to throw, which simulates a
    ///     transient transport failure (broker/database unavailable).
    /// </summary>
    public void FailNextReceives(int count)
    {
        Interlocked.Exchange(ref _receiveFaultCount, count);
    }

    public async Task<ReceivedMessage?> ReceiveAsync(IPipeline pipeline, CancellationToken cancellationToken = default)
    {
        if (Volatile.Read(ref _receiveFaultCount) > 0 && Interlocked.Decrement(ref _receiveFaultCount) >= 0)
        {
            throw new InvalidOperationException($"[simulated transient receive failure] : uri = '{Uri}'");
        }

        Stream stream;

        lock (_lock)
        {
            if (_queue.Count == 0)
            {
                return null;
            }

            stream = _queue.Dequeue();
        }

        var acknowledgementToken = Guid.NewGuid();

        _unacknowledged[acknowledgementToken] = stream;

        var result = new ReceivedMessage(await stream.CopyAsync(cancellationToken).ConfigureAwait(false), acknowledgementToken);

        await _hopperOptions.MessageReceived.InvokeAsync(new(this, result, pipeline), cancellationToken).ConfigureAwait(false);

        return result;
    }

    public ValueTask<bool> HasPendingAsync(CancellationToken cancellationToken = default)
    {
        return new(Count > 0);
    }

    public async Task AcknowledgeAsync(object acknowledgementToken, IPipeline pipeline, CancellationToken cancellationToken = default)
    {
        _unacknowledged.TryRemove((Guid)acknowledgementToken, out _);

        await _hopperOptions.MessageAcknowledged.InvokeAsync(new(this, acknowledgementToken, pipeline), cancellationToken).ConfigureAwait(false);
    }

    public async Task ReleaseAsync(object acknowledgementToken, IPipeline pipeline, CancellationToken cancellationToken = default)
    {
        if (_unacknowledged.TryRemove((Guid)acknowledgementToken, out var stream))
        {
            lock (_lock)
            {
                _queue.Enqueue(stream);
            }
        }

        await _hopperOptions.MessageReleased.InvokeAsync(new(this, acknowledgementToken, pipeline), cancellationToken).ConfigureAwait(false);
    }
}
