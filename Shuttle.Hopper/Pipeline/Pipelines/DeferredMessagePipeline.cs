using Shuttle.Core.Contract;
using Shuttle.Core.Pipelines;

namespace Shuttle.Hopper;

public class DeferredMessagePipeline : Pipeline
{
    public DeferredMessagePipeline(IPipelineDependencies pipelineDependencies, IServiceBus serviceBus, IReceiveDeferredMessageObserver receiveDeferredMessageObserver, IDeserializeTransportMessageObserver deserializeTransportMessageObserver, IProcessDeferredMessageObserver processDeferredMessageObserver)
        : base(pipelineDependencies)
    {
        Guard.AgainstNull(serviceBus);
        Guard.AgainstNull(serviceBus.Inbox);

        State.SetWorkTransport(Guard.AgainstNull(serviceBus.Inbox!.WorkTransport));
        State.SetErrorTransport(Guard.AgainstNull(serviceBus.Inbox.ErrorTransport));
        State.SetDeferredTransport(Guard.AgainstNull(serviceBus.Inbox.DeferredTransport));

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