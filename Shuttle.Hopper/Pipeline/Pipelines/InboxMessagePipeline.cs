using Microsoft.Extensions.Options;
using Shuttle.Core.Contract;
using Shuttle.Core.Pipelines;

namespace Shuttle.Hopper;

public class InboxMessagePipeline : Pipeline
{
    public InboxMessagePipeline(IOptions<PipelineOptions> pipelineOptions, IServiceProvider serviceProvider, IOptions<ServiceBusOptions> serviceBusOptions, IServiceBusConfiguration serviceBusConfiguration, IGetWorkMessageObserver getWorkMessageObserver, IDeserializeTransportMessageObserver deserializeTransportMessageObserver, IDeferTransportMessageObserver deferTransportMessageObserver, IDeserializeMessageObserver deserializeMessageObserver, IDecryptMessageObserver decryptMessageObserver, IDecompressMessageObserver decompressMessageObserver, IHandleMessageObserver handleMessageObserver, IAcknowledgeMessageObserver acknowledgeMessageObserver, IReceiveExceptionObserver receiveExceptionObserver)
        : base(pipelineOptions, serviceProvider)
    {
        AddStage("Read")
            .WithEvent<OnGetMessage>()
            .WithEvent<OnAfterGetMessage>()
            .WithEvent<OnDeserializeTransportMessage>()
            .WithEvent<OnAfterDeserializeTransportMessage>()
            .WithEvent<OnDecompressMessage>()
            .WithEvent<OnAfterDecompressMessage>()
            .WithEvent<OnDecryptMessage>()
            .WithEvent<OnAfterDecryptMessage>()
            .WithEvent<OnDeserializeMessage>()
            .WithEvent<OnAfterDeserializeMessage>();

        AddStage("Handle")
            .WithEvent<OnHandleMessage>()
            .WithEvent<OnAfterHandleMessage>()
            .WithEvent<OnAcknowledgeMessage>()
            .WithEvent<OnAfterAcknowledgeMessage>();

        AddObserver(Guard.AgainstNull(getWorkMessageObserver));
        AddObserver(Guard.AgainstNull(deserializeTransportMessageObserver));
        AddObserver(Guard.AgainstNull(deferTransportMessageObserver));
        AddObserver(Guard.AgainstNull(deserializeMessageObserver));
        AddObserver(Guard.AgainstNull(decryptMessageObserver));
        AddObserver(Guard.AgainstNull(decompressMessageObserver));
        AddObserver(Guard.AgainstNull(handleMessageObserver));
        AddObserver(Guard.AgainstNull(acknowledgeMessageObserver));

        AddObserver(Guard.AgainstNull(receiveExceptionObserver)); // must be last

        Guard.AgainstNull(Guard.AgainstNull(serviceBusOptions).Value);
        Guard.AgainstNull(serviceBusConfiguration);

        State.SetWorkTransport(Guard.AgainstNull(serviceBusConfiguration.Inbox!.WorkTransport));
        State.SetDeferredTransport(serviceBusConfiguration.Inbox.DeferredTransport);
        State.SetErrorTransport(serviceBusConfiguration.Inbox.ErrorTransport);

        State.SetDurationToIgnoreOnFailure(serviceBusOptions.Value.Inbox!.DurationToIgnoreOnFailure);
        State.SetMaximumFailureCount(serviceBusOptions.Value.Inbox.MaximumFailureCount);
    }
}