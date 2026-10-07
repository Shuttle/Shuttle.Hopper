using Microsoft.Extensions.Options;
using Shuttle.Contract;
using Shuttle.Pipelines;

namespace Shuttle.Hopper;

public interface IDeferredMessagePipeline : IPipeline;

public class DeferredMessagePipeline : Pipeline, IDeferredMessagePipeline
{
    public DeferredMessagePipeline(IOptions<PipelineOptions> pipelineOptions, IServiceProvider serviceProvider, IBusConfiguration busConfiguration)
        : base(pipelineOptions, serviceProvider)
    {
        Guard.AgainstNull(busConfiguration);

        var inbox = Guard.AgainstNull(busConfiguration.Inbox);

        // The primary inbox may have no deferred transport while an additional inbox has its own, in which case the
        // additional inbox's deferred message processor binds the state.
        if (inbox.HasDeferredTransport())
        {
            State.BindDeferredInbox(inbox, busConfiguration.AdditionalInboxes.Values
                .Where(item => item.DeferredTransport == inbox.DeferredTransport)
                .Select(item => Guard.AgainstNull(item.WorkTransport)));
        }

        AddStage("Process")
            .WithEvent<ReceiveMessage>()
            .WithEvent<MessageReceived>()
            .WithEvent<DeserializeTransportMessage>()
            .WithEvent<TransportMessageDeserialized>()
            .WithEvent<ProcessDeferredMessage>()
            .WithEvent<DeferredMessageProcessed>();

        AddObserver<IReceiveDeferredMessageObserver>();
        AddObserver<IDeserializeTransportMessageObserver>();
        AddObserver<IProcessDeferredMessageObserver>();

        AddObserver<IDeferredMessagePipelineFailedObserver>(ObserverPosition.End);
    }
}