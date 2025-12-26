using Microsoft.Extensions.Options;
using Shuttle.Core.Contract;
using Shuttle.Core.Pipelines;

namespace Shuttle.Hopper;

public class InboxMessagePipeline : Pipeline
{
    public InboxMessagePipeline(IPipelineDependencies pipelineDependencies, IOptions<ServiceBusOptions> serviceBusOptions, IServiceBusConfiguration serviceBusConfiguration, IReceiveWorkMessageObserver receiveWorkMessageObserver, IDeserializeTransportMessageObserver deserializeTransportMessageObserver, IDeferTransportMessageObserver deferTransportMessageObserver, IDeserializeMessageObserver deserializeMessageObserver, IDecryptMessageObserver decryptMessageObserver, IDecompressMessageObserver decompressMessageObserver, IHandleMessageObserver handleMessageObserver, IAcknowledgeMessageObserver acknowledgeMessageObserver, IReceivePipelineFailedObserver receivePipelineFailedObserver)
        : base(pipelineDependencies)
    {
        AddStage("Read")
            .WithEvent<ReceiveMessage>()
            .WithEvent<MessageReceived>()
            .WithEvent<DeserializeTransportMessage>()
            .WithEvent<TransportMessageDeserialized>()
            .WithEvent<DecompressMessage>()
            .WithEvent<MessageDecompressed>()
            .WithEvent<DecryptMessage>()
            .WithEvent<MessageDecrypted>()
            .WithEvent<DeserializeMessage>()
            .WithEvent<MessageDeserialized>();

        AddStage("Handle")
            .WithEvent<HandleMessage>()
            .WithEvent<MessageHandled>()
            .WithEvent<CompleteTransactionScope>()
            .WithEvent<DisposeTransactionScope>()
            .WithEvent<AcknowledgeMessage>()
            .WithEvent<MessageAcknowledged>();

        AddObserver(Guard.AgainstNull(receiveWorkMessageObserver));
        AddObserver(Guard.AgainstNull(deserializeTransportMessageObserver));
        AddObserver(Guard.AgainstNull(deferTransportMessageObserver));
        AddObserver(Guard.AgainstNull(deserializeMessageObserver));
        AddObserver(Guard.AgainstNull(decryptMessageObserver));
        AddObserver(Guard.AgainstNull(decompressMessageObserver));
        AddObserver(Guard.AgainstNull(handleMessageObserver));
        AddObserver(Guard.AgainstNull(acknowledgeMessageObserver));

        AddObserver(Guard.AgainstNull(receivePipelineFailedObserver)); // must be last

        Guard.AgainstNull(Guard.AgainstNull(serviceBusOptions).Value);
        Guard.AgainstNull(serviceBusConfiguration);

        State.SetWorkTransport(Guard.AgainstNull(serviceBusConfiguration.Inbox!.WorkTransport));
        State.SetDeferredTransport(serviceBusConfiguration.Inbox.DeferredTransport);
        State.SetErrorTransport(serviceBusConfiguration.Inbox.ErrorTransport);

        State.SetDurationToIgnoreOnFailure(serviceBusOptions.Value.Inbox.IgnoreOnFailureDurations);
        State.SetMaximumFailureCount(serviceBusOptions.Value.Inbox.MaximumFailureCount);
    }
}