using System.Text.Json;
using Microsoft.Extensions.Options;
using Shuttle.Contract;
using Shuttle.Pipelines;
using Shuttle.Serialization;
using Shuttle.Streams;
using JsonSerializer = Shuttle.Serialization.JsonSerializer;

namespace Shuttle.Hopper.Tests;

public class FakeTransport(HopperOptions hopperOptions, int messagesToReturn) : ITransport
{
    private readonly ISerializer _serializer = new JsonSerializer(Options.Create(new JsonSerializerOptions()));

    public int MessageCount { get; private set; }

    public int MessagesToReturn { get; } = messagesToReturn;

    public TransportType Type { get; } = TransportType.Queue;
    public TransportUri Uri { get; } = new(new Uri("fake://configuration/transport"));

    public async Task SendAsync(Stream stream, IState state, CancellationToken cancellationToken = default)
    {
        await hopperOptions.MessageSent.InvokeAsync(new(this, Guard.AgainstNull(Guard.AgainstNull(state).GetTransportMessage()), stream), cancellationToken).ConfigureAwait(false);
    }

    public ValueTask<bool> HasPendingAsync(CancellationToken cancellationToken = default)
    {
        return new(false);
    }

    public async Task AcknowledgeAsync(object acknowledgementToken, CancellationToken cancellationToken = default)
    {
        await hopperOptions.MessageAcknowledged.InvokeAsync(new(this, acknowledgementToken), cancellationToken).ConfigureAwait(false);
    }

    public async Task ReleaseAsync(object acknowledgementToken, CancellationToken cancellationToken = default)
    {
        await hopperOptions.MessageReleased.InvokeAsync(new(this, acknowledgementToken), cancellationToken).ConfigureAwait(false);
    }

    public async Task<ReceivedMessage?> ReceiveAsync(CancellationToken cancellationToken = default)
    {
        if (MessageCount == MessagesToReturn)
        {
            return null;
        }

        var expired = MessageCount % 2 != 0;

        var command = new SimpleCommand(expired ? "Expired" : "HasNotExpired");

        var transportMessage = new TransportMessage
        {
            MessageType = command.GetType().Name,
            ExpiresAt = expired ? DateTimeOffset.UtcNow.AddMilliseconds(-1) : DateTimeOffset.MaxValue,
            PrincipalIdentityName = "Identity",
            AssemblyQualifiedName = command.GetType().AssemblyQualifiedName!,
            Message = await (await _serializer.SerializeAsync(command, cancellationToken)).ToBytesAsync().ConfigureAwait(false)
        };

        MessageCount += 1;

        var result = new ReceivedMessage(await _serializer.SerializeAsync(transportMessage, cancellationToken).ConfigureAwait(false), string.Empty);

        await hopperOptions.MessageReceived.InvokeAsync(new(this, result), cancellationToken).ConfigureAwait(false);

        return result;
    }
}