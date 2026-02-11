using Microsoft.Extensions.Options;
using Shuttle.Core.Contract;
using Shuttle.Core.Pipelines;
using Shuttle.Core.TransactionScope;

namespace Shuttle.Hopper;

public class DispatchTransportMessagePipeline : Pipeline
{
    private readonly IMessageSenderContext _messageSenderContext;

    public DispatchTransportMessagePipeline(IOptions<PipelineOptions> pipelineOptions, IOptions<TransactionScopeOptions> transactionScopeOptions, ITransactionScopeFactory transactionScopeFactory, IServiceProvider serviceProvider, IMessageSenderContext messageSenderContext)
        : base(pipelineOptions, transactionScopeOptions, transactionScopeFactory, serviceProvider)
    {
        _messageSenderContext = Guard.AgainstNull(messageSenderContext);

        AddStage("Send")
            .WithEvent<FindMessageRoute>()
            .WithEvent<MessageRouteFound>()
            .WithEvent<SerializeTransportMessage>()
            .WithEvent<TransportMessageSerialized>()
            .WithEvent<DispatchTransportMessage>()
            .WithEvent<TransportMessageDispatched>();

        AddObserver<IFindMessageRouteObserver>();
        AddObserver<ISerializeTransportMessageObserver>();
        AddObserver<IDispatchTransportMessageObserver>();
    }

    public async Task<bool> ExecuteAsync(TransportMessage transportMessage, CancellationToken cancellationToken = default)
    {
        State.SetTransportMessage(Guard.AgainstNull(transportMessage));
        State.SetTransportMessageReceived(_messageSenderContext.TransportMessage);

        return await base.ExecuteAsync(cancellationToken);
    }
}