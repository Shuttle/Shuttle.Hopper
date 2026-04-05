using Microsoft.Extensions.Options;
using Shuttle.Core.Contract;
using Shuttle.Core.Pipelines;

namespace Shuttle.Hopper;

public interface IDeferredMessagePipeline : IPipeline;

public class DeferredMessagePipeline : Pipeline, IDeferredMessagePipeline
{
    public DeferredMessagePipeline(IOptions<PipelineOptions> pipelineOptions, IServiceProvider serviceProvider, IBusConfiguration busConfiguration)
        : base(pipelineOptions, serviceProvider)
    {
        Guard.AgainstNull(busConfiguration);
        Guard.AgainstNull(busConfiguration.Inbox);

        State.SetWorkTransport(Guard.AgainstNull(busConfiguration.Inbox!.WorkTransport));
        State.SetErrorTransport(Guard.AgainstNull(busConfiguration.Inbox.ErrorTransport));
        State.SetDeferredTransport(Guard.AgainstNull(busConfiguration.Inbox.DeferredTransport));

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
    }
}