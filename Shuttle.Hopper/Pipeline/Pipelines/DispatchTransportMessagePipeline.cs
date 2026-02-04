using Microsoft.Extensions.Options;
using Shuttle.Core.Contract;
using Shuttle.Core.Pipelines;
using Shuttle.Core.TransactionScope;

namespace Shuttle.Hopper;

public class DispatchTransportMessagePipeline : Pipeline
{
    public DispatchTransportMessagePipeline(IOptions<PipelineOptions> pipelineOptions, IOptions<TransactionScopeOptions> transactionScopeOptions, ITransactionScopeFactory transactionScopeFactory, IServiceProvider serviceProvider, IFindMessageRouteObserver findMessageRouteObserver, ISerializeTransportMessageObserver serializeTransportMessageObserver, IDispatchTransportMessageObserver dispatchTransportMessageObserver)
        : base(pipelineOptions, transactionScopeOptions, transactionScopeFactory, serviceProvider)
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