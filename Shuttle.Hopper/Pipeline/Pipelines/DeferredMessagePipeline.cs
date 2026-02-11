using Microsoft.Extensions.Options;
using Shuttle.Core.Contract;
using Shuttle.Core.Pipelines;
using Shuttle.Core.TransactionScope;

namespace Shuttle.Hopper;

public class DeferredMessagePipeline : Pipeline
{
    public DeferredMessagePipeline(IOptions<PipelineOptions> pipelineOptions, IOptions<TransactionScopeOptions> transactionScopeOptions, ITransactionScopeFactory transactionScopeFactory, IServiceProvider serviceProvider, IBusConfiguration busConfiguration)
        : base(pipelineOptions, transactionScopeOptions, transactionScopeFactory, serviceProvider)
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
        AddObserver< IProcessDeferredMessageObserver>();
    }
}