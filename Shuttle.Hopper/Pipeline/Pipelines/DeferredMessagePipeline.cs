using Shuttle.Core.Contract;
using Shuttle.Core.Pipelines;

namespace Shuttle.Hopper;

public class DeferredMessagePipeline : Pipeline
{
    public DeferredMessagePipeline(IPipelineDependencies pipelineDependencies, IServiceBusConfiguration serviceBusConfiguration, IReceiveDeferredMessageObserver receiveDeferredMessageObserver, IDeserializeTransportMessageObserver deserializeTransportMessageObserver, IProcessDeferredMessageObserver processDeferredMessageObserver)
        : base(pipelineDependencies)
    {
        Guard.AgainstNull(serviceBusConfiguration);
        Guard.AgainstNull(serviceBusConfiguration.Inbox);

        State.SetWorkTransport(Guard.AgainstNull(serviceBusConfiguration.Inbox!.WorkTransport));
        State.SetErrorTransport(Guard.AgainstNull(serviceBusConfiguration.Inbox.ErrorTransport));
        State.SetDeferredTransport(Guard.AgainstNull(serviceBusConfiguration.Inbox.DeferredTransport));

        AddStage("Process")
            .WithEvent<ReceiveMessage>()
            .WithEvent<MessageReceived>()
            .WithEvent<DeserializeTransportMessage>()
            .WithEvent<TransportMessageDeserialized>()
            .WithEvent<ProcessDeferredMessage>()
            .WithEvent<DeferredMessageProcessed>();

        AddObserver(Guard.AgainstNull(receiveDeferredMessageObserver));
        AddObserver(Guard.AgainstNull(deserializeTransportMessageObserver));
        AddObserver(Guard.AgainstNull(processDeferredMessageObserver));
    }
}