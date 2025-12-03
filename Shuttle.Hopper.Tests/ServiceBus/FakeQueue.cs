using System.Text.Json;
using Microsoft.Extensions.Options;
using Shuttle.Core.Contract;
using Shuttle.Core.Serialization;
using Shuttle.Core.Streams;
using JsonSerializer = Shuttle.Core.Serialization.JsonSerializer;

namespace Shuttle.Hopper.Tests;

public class FakeTransport(ServiceBusOptions serviceBusOptions, int messagesToReturn) : ITransport
{
    private readonly ISerializer _serializer = new JsonSerializer(Options.Create(new JsonSerializerOptions()));
    private readonly ServiceBusOptions _serviceBusOptions = Guard.AgainstNull(serviceBusOptions);

    public int MessageCount { get; private set; }

    public int MessagesToReturn { get; } = messagesToReturn;

    public TransportType Type { get; } = TransportType.Queue;
    public TransportUri Uri { get; } = new(new Uri("fake://configuration/transport"));

    public async Task SendAsync(TransportMessage transportMessage, Stream stream, CancellationToken cancellationToken = default)
    {
        await serviceBusOptions.MessageEntransportd.InvokeAsync(new(transportMessage, stream), cancellationToken).ConfigureAwait(false);
    }

    public async Task AcknowledgeAsync(object acknowledgementToken, CancellationToken cancellationToken = default)
    {
        await serviceBusOptions.MessageAcknowledged.InvokeAsync(new(acknowledgementToken), cancellationToken).ConfigureAwait(false);
    }

    public async Task ReleaseAsync(object acknowledgementToken, CancellationToken cancellationToken = default)
    {
        await serviceBusOptions.MessageReleased.InvokeAsync(new(acknowledgementToken), cancellationToken).ConfigureAwait(false);
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
            ExpiryDate = expired ? DateTime.Now.AddMilliseconds(-1) : DateTime.MaxValue,
            PrincipalIdentityName = "Identity",
            AssemblyQualifiedName = command.GetType().AssemblyQualifiedName!,
            Message = await (await _serializer.SerializeAsync(command)).ToBytesAsync().ConfigureAwait(false)
        };

        MessageCount += 1;

        var result = new ReceivedMessage(await _serializer.SerializeAsync(transportMessage).ConfigureAwait(false), string.Empty);

        await serviceBusOptions.MessageReceived.InvokeAsync(new(result), cancellationToken).ConfigureAwait(false);

        return result;
    }
}