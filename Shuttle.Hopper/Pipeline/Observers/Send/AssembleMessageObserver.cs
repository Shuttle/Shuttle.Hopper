using Microsoft.Extensions.Options;
using Shuttle.Core.Contract;
using Shuttle.Core.Pipelines;

namespace Shuttle.Hopper;

public interface IAssembleMessageObserver : IPipelineObserver<AssembleMessage>;

public class AssembleMessageObserver(IOptions<HopperOptions> serviceBusOptions, IServiceBusConfiguration serviceBusConfiguration, IIdentityProvider identityProvider)
    : IAssembleMessageObserver
{
    private readonly IServiceBusConfiguration _serviceBusConfiguration = Guard.AgainstNull(serviceBusConfiguration);
    private readonly IIdentityProvider _identityProvider = Guard.AgainstNull(identityProvider);
    private readonly HopperOptions _hopperOptions = Guard.AgainstNull(Guard.AgainstNull(serviceBusOptions).Value);

    public Task ExecuteAsync(IPipelineContext<AssembleMessage> pipelineContext, CancellationToken cancellationToken = default)
    {
        var state = Guard.AgainstNull(pipelineContext).Pipeline.State;
        var builder = state.GetTransportMessageBuilder();
        var message = Guard.AgainstNull(state.GetMessage());
        var transportMessageReceived = state.GetTransportMessageReceived();

        var identity = _identityProvider.Get();

        var transportMessage = new TransportMessage
        {
            SenderInboxWorkTransportUri = _serviceBusConfiguration.HasInbox()
                ? Guard.AgainstNull(_serviceBusConfiguration.Inbox!.WorkTransport).Uri.ToString()
                : string.Empty,
            PrincipalIdentityName = Guard.AgainstNull(Guard.AgainstNull(identity).Name),
            MessageType = Guard.AgainstEmpty(message.GetType().FullName),
            AssemblyQualifiedName = Guard.AgainstEmpty(message.GetType().AssemblyQualifiedName),
            EncryptionAlgorithm = _hopperOptions.EncryptionAlgorithm,
            CompressionAlgorithm = _hopperOptions.CompressionAlgorithm,
            SentAt = DateTimeOffset.UtcNow
        };

        if (transportMessageReceived != null)
        {
            transportMessage.MessageReceivedId = transportMessageReceived.MessageId;
            transportMessage.CorrelationId = transportMessageReceived.CorrelationId;
            transportMessage.Headers.AddRange(transportMessageReceived.Headers);
        }

        var transportMessageBuilder = new TransportMessageBuilder(transportMessage);

        builder?.Invoke(transportMessageBuilder);

        if (transportMessageBuilder.ShouldSendToSelf)
        {
            if (!_serviceBusConfiguration.HasInbox())
            {
                throw new InvalidOperationException(Resources.SendToSelfException);
            }

            transportMessage.RecipientInboxWorkTransportUri = Guard.AgainstNull(_serviceBusConfiguration.Inbox!.WorkTransport).Uri.ToString();
        }

        if (transportMessageBuilder.ShouldReply)
        {
            if (transportMessageReceived == null || string.IsNullOrEmpty(transportMessageReceived.SenderInboxWorkTransportUri))
            {
                throw new InvalidOperationException(Resources.SendReplyException);
            }

            transportMessage.RecipientInboxWorkTransportUri = transportMessageReceived.SenderInboxWorkTransportUri;
        }

        if (transportMessage.IgnoreUntil > DateTimeOffset.UtcNow &&
            _serviceBusConfiguration.HasInbox() &&
            Guard.AgainstNull(_serviceBusConfiguration.Inbox!.WorkTransport).Type == TransportType.Stream)
        {
            throw new InvalidOperationException(Resources.DeferStreamException);
        }

        state.SetTransportMessage(transportMessage);

        return Task.CompletedTask;
    }
}