using Microsoft.Extensions.Options;
using Shuttle.Core.Contract;
using Shuttle.Core.Pipelines;

namespace Shuttle.Hopper;

public interface IInboxMessagePipeline : IPipeline;

public class InboxMessagePipeline : Pipeline, IInboxMessagePipeline
{
    public InboxMessagePipeline(IOptions<PipelineOptions> pipelineOptions, IPipelineState pipelineState, IServiceProvider serviceProvider, IOptions<HopperOptions> hopperOptions, IBusConfiguration busConfiguration)
        : base(pipelineOptions, pipelineState, serviceProvider)
    {
        AddStage("Read")
            .WithEvent<ReceiveMessage>()
            .WithEvent<MessageReceived>()
            .WithEvent<DeserializeTransportMessage>()
            .WithEvent<TransportMessageDeserialized>()
            .WithEvent<DeserializeMessage>()
            .WithEvent<MessageDeserialized>();

        AddStage("Handle")
            .WithEvent<HandleMessage>()
            .WithEvent<MessageHandled>()
            .WithEvent<AcknowledgeMessage>()
            .WithEvent<MessageAcknowledged>();

        AddObserver<IReceiveWorkMessageObserver>();
        AddObserver<IDeserializeTransportMessageObserver>();
        AddObserver<IDeferTransportMessageObserver>();
        AddObserver<IDeserializeMessageObserver>();
        AddObserver<IHandleMessageObserver>();
        AddObserver<IAcknowledgeMessageObserver>();

        AddObserver<IReceivePipelineFailedObserver>(ObserverPosition.End);

        Guard.AgainstNull(Guard.AgainstNull(hopperOptions).Value);
        Guard.AgainstNull(busConfiguration);

        State.SetWorkTransport(Guard.AgainstNull(busConfiguration.Inbox!.WorkTransport));
        State.SetDeferredTransport(busConfiguration.Inbox.DeferredTransport);
        State.SetErrorTransport(busConfiguration.Inbox.ErrorTransport);

        State.SetDurationToIgnoreOnFailure(hopperOptions.Value.Inbox.IgnoreOnFailureDurations.Count > 0
            ? hopperOptions.Value.Inbox.IgnoreOnFailureDurations 
            : HopperOptions.DefaultIgnoreOnFailureDurations);
        State.SetMaximumFailureCount(hopperOptions.Value.Inbox.MaximumFailureCount);
    }
}