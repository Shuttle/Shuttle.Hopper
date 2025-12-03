using Microsoft.Extensions.Options;
using Shuttle.Core.Contract;
using Shuttle.Core.Pipelines;

namespace Shuttle.Hopper;

public class DeferredMessagePipeline : Pipeline
{
    public DeferredMessagePipeline(IOptions<PipelineOptions> pipelineOptions, IServiceProvider serviceProvider, IServiceBusConfiguration serviceBusConfiguration, IGetDeferredMessageObserver getDeferredMessageObserver, IDeserializeTransportMessageObserver deserializeTransportMessageObserver, IProcessDeferredMessageObserver processDeferredMessageObserver)
        : base(pipelineOptions, serviceProvider)
    {
        Guard.AgainstNull(serviceBusConfiguration);
        Guard.AgainstNull(serviceBusConfiguration.Inbox);

        State.SetWorkTransport(Guard.AgainstNull(serviceBusConfiguration.Inbox!.WorkTransport));
        State.SetErrorTransport(Guard.AgainstNull(serviceBusConfiguration.Inbox.ErrorTransport));
        State.SetDeferredTransport(Guard.AgainstNull(serviceBusConfiguration.Inbox.DeferredTransport));

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