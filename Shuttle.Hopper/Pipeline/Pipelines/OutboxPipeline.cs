using Microsoft.Extensions.Options;
using Shuttle.Core.Contract;
using Shuttle.Core.Pipelines;
using Shuttle.Core.TransactionScope;

namespace Shuttle.Hopper;

public class OutboxPipeline : Pipeline
{
    public OutboxPipeline(IOptions<PipelineOptions> pipelineOptions, IOptions<TransactionScopeOptions> transactionScopeOptions, ITransactionScopeFactory transactionScopeFactory, IServiceProvider serviceProvider, IOptions<HopperOptions> hopperOptions, IBusConfiguration busConfiguration)
        : base(pipelineOptions, transactionScopeOptions, transactionScopeFactory, serviceProvider)
    {
        Guard.AgainstNull(Guard.AgainstNull(hopperOptions).Value);

        if (busConfiguration.Outbox == null)
        {
            return;
        }

        State.SetWorkTransport(Guard.AgainstNull(busConfiguration.Outbox.WorkTransport));
        State.SetErrorTransport(busConfiguration.Outbox.ErrorTransport);

        State.SetDurationToIgnoreOnFailure(hopperOptions.Value.Outbox.IgnoreOnFailureDurations);
        State.SetMaximumFailureCount(hopperOptions.Value.Outbox.MaximumFailureCount);

        AddStage("Read")
            .WithEvent<ReceiveMessage>()
            .WithEvent<MessageReceived>()
            .WithEvent<DeserializeTransportMessage>()
            .WithEvent<TransportMessageDeserialized>();

        AddStage("Send")
            .WithEvent<DispatchTransportMessage>()
            .WithEvent<TransportMessageDispatched>()
            .WithEvent<MessageAcknowledged>();

        AddObserver<IReceiveWorkMessageObserver>();
        AddObserver<IDeserializeTransportMessageObserver>();
        AddObserver<ISendOutboxMessageObserver>();
        AddObserver<IAcknowledgeMessageObserver>();
        AddObserver<IOutboxExceptionObserver>(); // must be last
    }
}