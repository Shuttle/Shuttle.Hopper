using Microsoft.Extensions.Options;
using Shuttle.Core.Contract;
using Shuttle.Core.Pipelines;
using Shuttle.Core.TransactionScope;

namespace Shuttle.Hopper;

public class TransportMessagePipeline : Pipeline
{
    public TransportMessagePipeline(IOptions<PipelineOptions> pipelineOptions, IOptions<TransactionScopeOptions> transactionScopeOptions, ITransactionScopeFactory transactionScopeFactory, IServiceProvider serviceProvider, IAssembleMessageObserver assembleMessageObserver, ISerializeMessageObserver serializeMessageObserver, ICompressMessageObserver compressMessageObserver, IEncryptMessageObserver encryptMessageObserver)
        : base(pipelineOptions, transactionScopeOptions, transactionScopeFactory, serviceProvider)
    {
        AddStage("Create")
            .WithEvent<AssembleMessage>()
            .WithEvent<MessageAssembled>()
            .WithEvent<SerializeMessage>()
            .WithEvent<MessageSerialized>()
            .WithEvent<EncryptMessage>()
            .WithEvent<MessageEncrypted>()
            .WithEvent<CompressMessage>()
            .WithEvent<MessageCompressed>();

        AddObserver(Guard.AgainstNull(assembleMessageObserver));
        AddObserver(Guard.AgainstNull(serializeMessageObserver));
        AddObserver(Guard.AgainstNull(compressMessageObserver));
        AddObserver(Guard.AgainstNull(encryptMessageObserver));
    }

    public async Task<bool> ExecuteAsync(object message, TransportMessage? transportMessageReceived, Action<TransportMessageBuilder>? builder, CancellationToken cancellationToken = default)
    {
        State.SetMessage(Guard.AgainstNull(message));
        State.SetTransportMessageReceived(transportMessageReceived);
        State.SetTransportMessageBuilder(builder);

        return await base.ExecuteAsync(cancellationToken).ConfigureAwait(false);
    }
}