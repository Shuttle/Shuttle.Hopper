using Microsoft.Extensions.Options;
using Shuttle.Core.Contract;
using Shuttle.Core.Pipelines;
using Shuttle.Core.TransactionScope;

namespace Shuttle.Hopper;

public class InboxMessagePipeline : Pipeline
{
    public InboxMessagePipeline(IOptions<PipelineOptions> pipelineOptions, IOptions<TransactionScopeOptions> transactionScopeOptions, ITransactionScopeFactory transactionScopeFactory, IServiceProvider serviceProvider, IOptions<HopperOptions> hopperOptions, IBusConfiguration busConfiguration, IReceiveWorkMessageObserver receiveWorkMessageObserver, IDeserializeTransportMessageObserver deserializeTransportMessageObserver, IDeferTransportMessageObserver deferTransportMessageObserver, IDeserializeMessageObserver deserializeMessageObserver, IDecryptMessageObserver decryptMessageObserver, IDecompressMessageObserver decompressMessageObserver, IHandleMessageObserver handleMessageObserver, IAcknowledgeMessageObserver acknowledgeMessageObserver, IReceivePipelineFailedObserver receivePipelineFailedObserver)
        : base(pipelineOptions, transactionScopeOptions, transactionScopeFactory, serviceProvider)
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
            .WithEvent<MessageDeserialized>()
            .WithTransactionScope();

        AddStage("Handle")
            .WithEvent<HandleMessage>()
            .WithEvent<MessageHandled>()
            .WithEvent<CompleteTransactionScope>()
            .WithEvent<DisposeTransactionScope>()
            .WithEvent<AcknowledgeMessage>()
            .WithEvent<MessageAcknowledged>()
            .WithTransactionScope();

        AddObserver(Guard.AgainstNull(receiveWorkMessageObserver));
        AddObserver(Guard.AgainstNull(deserializeTransportMessageObserver));
        AddObserver(Guard.AgainstNull(deferTransportMessageObserver));
        AddObserver(Guard.AgainstNull(deserializeMessageObserver));
        AddObserver(Guard.AgainstNull(decryptMessageObserver));
        AddObserver(Guard.AgainstNull(decompressMessageObserver));
        AddObserver(Guard.AgainstNull(handleMessageObserver));
        AddObserver(Guard.AgainstNull(acknowledgeMessageObserver));

        AddObserver(Guard.AgainstNull(receivePipelineFailedObserver)); // must be last

        Guard.AgainstNull(Guard.AgainstNull(hopperOptions).Value);
        Guard.AgainstNull(busConfiguration);

        State.SetWorkTransport(Guard.AgainstNull(busConfiguration.Inbox!.WorkTransport));
        State.SetDeferredTransport(busConfiguration.Inbox.DeferredTransport);
        State.SetErrorTransport(busConfiguration.Inbox.ErrorTransport);

        State.SetDurationToIgnoreOnFailure(hopperOptions.Value.Inbox.IgnoreOnFailureDurations);
        State.SetMaximumFailureCount(hopperOptions.Value.Inbox.MaximumFailureCount);
    }
}