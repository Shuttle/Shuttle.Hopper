using Microsoft.Extensions.Options;
using Shuttle.Contract;
using Shuttle.Pipelines;

namespace Shuttle.Hopper;

public interface IDispatchTransportMessagePipeline : IPipeline
{
    Task<bool> ExecuteAsync(TransportMessage transportMessage, CancellationToken cancellationToken = default);
}

public class DispatchTransportMessagePipeline : Pipeline, IDispatchTransportMessagePipeline
{
    private readonly IMessageSenderContext _messageSenderContext;

    public DispatchTransportMessagePipeline(IOptions<PipelineOptions> pipelineOptions, IServiceProvider serviceProvider, IMessageSenderContext messageSenderContext)
        : base(pipelineOptions, serviceProvider)
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