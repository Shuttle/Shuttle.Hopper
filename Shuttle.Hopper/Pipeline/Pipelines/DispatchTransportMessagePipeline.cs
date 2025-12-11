using Shuttle.Core.Contract;
using Shuttle.Core.Pipelines;

namespace Shuttle.Hopper;

public class DispatchTransportMessagePipeline : Pipeline
{
    public DispatchTransportMessagePipeline(IPipelineDependencies pipelineDependencies, IFindMessageRouteObserver findMessageRouteObserver, ISerializeTransportMessageObserver serializeTransportMessageObserver, IDispatchTransportMessageObserver dispatchTransportMessageObserver)
        : base(pipelineDependencies)
    {
        AddStage("Send")
            .WithEvent<FindMessageRoute>()
            .WithEvent<MessageRouteFound>()
            .WithEvent<SerializeTransportMessage>()
            .WithEvent<TransportMessageSerialized>()
            .WithEvent<DispatchTransportMessage>()
            .WithEvent<TransportMessageDispatched>();

        AddObserver(Guard.AgainstNull(findMessageRouteObserver));
        AddObserver(Guard.AgainstNull(serializeTransportMessageObserver));
        AddObserver(Guard.AgainstNull(dispatchTransportMessageObserver));
    }

    public async Task<bool> ExecuteAsync(TransportMessage transportMessage, TransportMessage? transportMessageReceived, CancellationToken cancellationToken = default)
    {
        State.SetTransportMessage(Guard.AgainstNull(transportMessage));
        State.SetTransportMessageReceived(transportMessageReceived);

        return await base.ExecuteAsync(cancellationToken);
    }
}