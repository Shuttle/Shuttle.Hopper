using Microsoft.Extensions.Options;
using Shuttle.Core.Contract;
using Shuttle.Core.Pipelines;

namespace Shuttle.Hopper;

public interface IOutboxPipeline : IPipeline;

public class OutboxPipeline : Pipeline, IOutboxPipeline
{
    public OutboxPipeline(IOptions<PipelineOptions> pipelineOptions, IPipelineState pipelineState, IServiceProvider serviceProvider, IOptions<HopperOptions> hopperOptions, IBusConfiguration busConfiguration)
        : base(pipelineOptions, pipelineState, serviceProvider)
    {
        Guard.AgainstNull(Guard.AgainstNull(hopperOptions).Value);

        if (busConfiguration.Outbox == null)
        {
            return;
        }

        State.SetWorkTransport(Guard.AgainstNull(busConfiguration.Outbox.WorkTransport));
        State.SetErrorTransport(busConfiguration.Outbox.ErrorTransport);

        State.SetDurationToIgnoreOnFailure(hopperOptions.Value.Outbox.IgnoreOnFailureDurations.Count > 0
            ? hopperOptions.Value.Outbox.IgnoreOnFailureDurations
            : HopperOptions.DefaultIgnoreOnFailureDurations);
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
        AddObserver<IOutboxExceptionObserver>(ObserverPosition.End);
    }
}