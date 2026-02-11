using Shuttle.Core.Contract;
using Shuttle.Core.Pipelines;
using Shuttle.Core.Threading;

namespace Shuttle.Hopper;

public class DeferredMessageProcessor(IPipelineFactory pipelineFactory, IDeferredMessageProcessorContext deferredMessageProcessorContext)
    : IProcessor
{
    private readonly IDeferredMessageProcessorContext _deferredMessageProcessorContext = Guard.AgainstNull(deferredMessageProcessorContext);
    private readonly IPipelineFactory _pipelineFactory = Guard.AgainstNull(pipelineFactory);

    public async ValueTask<bool> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        if (!_deferredMessageProcessorContext.ShouldCheckDeferredMessages)
        {
            return false;
        }

        var pipeline = await _pipelineFactory.GetPipelineAsync<DeferredMessagePipeline>(cancellationToken);

        pipeline.State.ResetReceivedMessage();
        pipeline.State.ResetDeferredMessageReturned();
        pipeline.State.SetTransportMessage(null);

        await pipeline.ExecuteAsync(cancellationToken).ConfigureAwait(false);

        return await _deferredMessageProcessorContext.GetResultAsync(pipeline.State, cancellationToken);
    }
}