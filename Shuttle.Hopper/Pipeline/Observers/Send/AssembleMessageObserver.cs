using Microsoft.Extensions.Options;
using Shuttle.Core.Contract;
using Shuttle.Core.Pipelines;

namespace Shuttle.Hopper;

public interface IAssembleMessageObserver : IPipelineObserver<AssembleMessage>;

public class AssembleMessageObserver(IOptions<ServiceBusOptions> serviceBusOptions, IServiceBus serviceBus, IIdentityProvider identityProvider)
    : IAssembleMessageObserver
{
    private readonly IServiceBus _serviceBus = Guard.AgainstNull(serviceBus);
    private readonly IIdentityProvider _identityProvider = Guard.AgainstNull(identityProvider);
    private readonly ServiceBusOptions _serviceBusOptions = Guard.AgainstNull(Guard.AgainstNull(serviceBusOptions).Value);

    public async Task ExecuteAsync(IPipelineContext<AssembleMessage> pipelineContext, CancellationToken cancellationToken = default)
    {
        var state = Guard.AgainstNull(pipelineContext).Pipeline.State;
        var builder = state.GetTransportMessageBuilder();
        var message = Guard.AgainstNull(state.GetMessage());
        var transportMessageReceived = state.GetTransportMessageReceived();

        var identity = _identityProvider.Get();

        var transportMessage = new TransportMessage
        {
            SenderInboxWorkTransportUri = _serviceBus.HasInbox()
                ? Guard.AgainstNull(_serviceBus.Inbox!.WorkTransport).Uri.ToString()
                : string.Empty,
            PrincipalIdentityName = Guard.AgainstNull(Guard.AgainstNull(identity).Name),
            MessageType = Guard.AgainstEmpty(message.GetType().FullName),
            AssemblyQualifiedName = Guard.AgainstEmpty(message.GetType().AssemblyQualifiedName),
            EncryptionAlgorithm = _serviceBusOptions.EncryptionAlgorithm,
            CompressionAlgorithm = _serviceBusOptions.CompressionAlgorithm,
            SendDate = DateTime.UtcNow
        };

        if (transportMessageReceived != null)
        {
            transportMessage.MessageReceivedId = transportMessageReceived.MessageId;
            transportMessage.CorrelationId = transportMessageReceived.CorrelationId;
            transportMessage.Headers.AddRange(transportMessageReceived.Headers);
        }

        var transportMessageBuilder = new TransportMessageBuilder(transportMessage);

        builder?.Invoke(transportMessageBuilder);

        if (transportMessageBuilder.ShouldSendLocal)
        {
            if (!_serviceBus.HasInbox())
            {
                throw new InvalidOperationException(Resources.SendToSelfException);
            }

            transportMessage.RecipientInboxWorkTransportUri = Guard.AgainstNull(_serviceBus.Inbox!.WorkTransport).Uri.ToString();
        }

        if (transportMessageBuilder.ShouldReply)
        {
            if (transportMessageReceived == null || string.IsNullOrEmpty(transportMessageReceived.SenderInboxWorkTransportUri))
            {
                throw new InvalidOperationException(Resources.SendReplyException);
            }

            transportMessage.RecipientInboxWorkTransportUri = transportMessageReceived.SenderInboxWorkTransportUri;
        }

        if (transportMessage.IgnoreTillDate > DateTime.UtcNow &&
            _serviceBus.HasInbox() &&
            Guard.AgainstNull(_serviceBus.Inbox!.WorkTransport).Type == TransportType.Stream)
        {
            throw new InvalidOperationException(Resources.DeferStreamException);
        }

        state.SetTransportMessage(transportMessage);

        await Task.CompletedTask;
    }
}