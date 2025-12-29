using System.Text.Json;
using Microsoft.Extensions.Options;
using Shuttle.Core.Serialization;
using Shuttle.Core.Streams;
using JsonSerializer = Shuttle.Core.Serialization.JsonSerializer;

namespace Shuttle.Hopper.Tests;

public class FakeTransport(ServiceBusOptions serviceBusOptions, int messagesToReturn) : ITransport
{
    private readonly ISerializer _serializer = new JsonSerializer(Options.Create(new JsonSerializerOptions()));

    public int MessageCount { get; private set; }

    public int MessagesToReturn { get; } = messagesToReturn;

    public TransportType Type { get; } = TransportType.Queue;
    public TransportUri Uri { get; } = new(new Uri("fake://configuration/transport"));

    public async Task SendAsync(TransportMessage transportMessage, Stream stream, CancellationToken cancellationToken = default)
    {
        await serviceBusOptions.MessageSent.InvokeAsync(new(this, transportMessage, stream), cancellationToken).ConfigureAwait(false);
    }

    public ValueTask<bool> HasPendingAsync(CancellationToken cancellationToken = default)
    {
        return new(false);
    }

    public async Task AcknowledgeAsync(object acknowledgementToken, CancellationToken cancellationToken = default)
    {
        await serviceBusOptions.MessageAcknowledged.InvokeAsync(new(this, acknowledgementToken), cancellationToken).ConfigureAwait(false);
    }

    public async Task ReleaseAsync(object acknowledgementToken, CancellationToken cancellationToken = default)
    {
        await serviceBusOptions.MessageReleased.InvokeAsync(new(this, acknowledgementToken), cancellationToken).ConfigureAwait(false);
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
            ExpiryDateTime = expired ? DateTimeOffset.UtcNow.AddMilliseconds(-1) : DateTimeOffset.MaxValue,
            PrincipalIdentityName = "Identity",
            AssemblyQualifiedName = command.GetType().AssemblyQualifiedName!,
            Message = await (await _serializer.SerializeAsync(command, cancellationToken)).ToBytesAsync().ConfigureAwait(false)
        };

        MessageCount += 1;

        var result = new ReceivedMessage(await _serializer.SerializeAsync(transportMessage, cancellationToken).ConfigureAwait(false), string.Empty);

        await serviceBusOptions.MessageReceived.InvokeAsync(new(this, result), cancellationToken).ConfigureAwait(false);

        return result;
    }
}