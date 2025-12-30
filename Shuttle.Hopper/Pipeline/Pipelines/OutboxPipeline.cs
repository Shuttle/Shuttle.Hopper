using Microsoft.Extensions.Options;
using Shuttle.Core.Contract;
using Shuttle.Core.Pipelines;

namespace Shuttle.Hopper;

public class OutboxPipeline : Pipeline
{
    public OutboxPipeline(IPipelineDependencies pipelineDependencies, IOptions<HopperOptions> serviceBusOptions, IServiceBusConfiguration serviceBusConfiguration, IReceiveWorkMessageObserver receiveWorkMessageObserver, IDeserializeTransportMessageObserver deserializeTransportMessageObserver, ISendOutboxMessageObserver sendOutboxMessageObserver, IAcknowledgeMessageObserver acknowledgeMessageObserver, IOutboxExceptionObserver outboxExceptionObserver)
        : base(pipelineDependencies)
    {
        Guard.AgainstNull(Guard.AgainstNull(serviceBusOptions).Value);

        if (serviceBusConfiguration.Outbox == null)
        {
            return;
        }

        State.SetWorkTransport(Guard.AgainstNull(serviceBusConfiguration.Outbox.WorkTransport));
        State.SetErrorTransport(serviceBusConfiguration.Outbox.ErrorTransport);

        State.SetDurationToIgnoreOnFailure(serviceBusOptions.Value.Outbox.IgnoreOnFailureDurations);
        State.SetMaximumFailureCount(serviceBusOptions.Value.Outbox.MaximumFailureCount);

        AddStage("Read")
            .WithEvent<ReceiveMessage>()
            .WithEvent<MessageReceived>()
            .WithEvent<DeserializeTransportMessage>()
            .WithEvent<TransportMessageDeserialized>();

        AddStage("Send")
            .WithEvent<DispatchTransportMessage>()
            .WithEvent<TransportMessageDispatched>()
            .WithEvent<MessageAcknowledged>();

        AddObserver(Guard.AgainstNull(receiveWorkMessageObserver));
        AddObserver(Guard.AgainstNull(deserializeTransportMessageObserver));
        AddObserver(Guard.AgainstNull(sendOutboxMessageObserver));
        AddObserver(Guard.AgainstNull(acknowledgeMessageObserver));

        AddObserver(Guard.AgainstNull(outboxExceptionObserver)); // must be last
    }
}