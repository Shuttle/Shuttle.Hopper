using Microsoft.Extensions.Options;
using Shuttle.Core.Contract;
using Shuttle.Core.Pipelines;

namespace Shuttle.Hopper;

public class DeferredMessagePipeline : Pipeline
{
    public DeferredMessagePipeline(IOptions<PipelineOptions> pipelineOptions, IServiceProvider serviceProvider, IServiceBus serviceBus, IGetDeferredMessageObserver getDeferredMessageObserver, IDeserializeTransportMessageObserver deserializeTransportMessageObserver, IProcessDeferredMessageObserver processDeferredMessageObserver)
        : base(pipelineOptions, serviceProvider)
    {
        Guard.AgainstNull(serviceBus);
        Guard.AgainstNull(serviceBus.Inbox);

        State.SetWorkTransport(Guard.AgainstNull(serviceBus.Inbox!.WorkTransport));
        State.SetErrorTransport(Guard.AgainstNull(serviceBus.Inbox.ErrorTransport));
        State.SetDeferredTransport(Guard.AgainstNull(serviceBus.Inbox.DeferredTransport));

        AddStage("Process")
            .WithEvent<OnGetMessage>()
            .WithEvent<OnAfterGetMessage>()
            .WithEvent<OnDeserializeTransportMessage>()
            .WithEvent<OnAfterDeserializeTransportMessage>()
            .WithEvent<OnProcessDeferredMessage>()
            .WithEvent<OnAfterProcessDeferredMessage>();

        AddObserver(Guard.AgainstNull(getDeferredMessageObserver));
        AddObserver(Guard.AgainstNull(deserializeTransportMessageObserver));
        AddObserver(Guard.AgainstNull(processDeferredMessageObserver));
    }
}