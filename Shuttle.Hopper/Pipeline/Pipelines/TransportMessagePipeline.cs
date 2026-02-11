using Microsoft.Extensions.Options;
using Shuttle.Core.Contract;
using Shuttle.Core.Pipelines;
using Shuttle.Core.TransactionScope;

namespace Shuttle.Hopper;

public class TransportMessagePipeline : Pipeline
{
    private readonly IMessageSenderContext _messageSenderContext;

    public TransportMessagePipeline(IOptions<PipelineOptions> pipelineOptions, IOptions<TransactionScopeOptions> transactionScopeOptions, ITransactionScopeFactory transactionScopeFactory, IServiceProvider serviceProvider, IMessageSenderContext messageSenderContext)
        : base(pipelineOptions, transactionScopeOptions, transactionScopeFactory, serviceProvider)
    {
        _messageSenderContext = Guard.AgainstNull(messageSenderContext);

        AddStage("Create")
            .WithEvent<AssembleMessage>()
            .WithEvent<MessageAssembled>()
            .WithEvent<SerializeMessage>()
            .WithEvent<MessageSerialized>()
            .WithEvent<EncryptMessage>()
            .WithEvent<MessageEncrypted>()
            .WithEvent<CompressMessage>()
            .WithEvent<MessageCompressed>();

        AddObserver<IAssembleMessageObserver>();
        AddObserver<ISerializeMessageObserver>();
        AddObserver<ICompressMessageObserver>();
        AddObserver<IEncryptMessageObserver>();
    }

    public async Task<bool> ExecuteAsync(object message, Action<TransportMessageBuilder>? builder, CancellationToken cancellationToken = default)
    {
        State.SetMessage(Guard.AgainstNull(message));
        State.SetTransportMessageReceived(_messageSenderContext.TransportMessage);
        State.SetTransportMessageBuilder(builder);

        return await base.ExecuteAsync(cancellationToken).ConfigureAwait(false);
    }
}