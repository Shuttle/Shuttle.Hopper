using Shuttle.Core.Contract;
using Shuttle.Core.Threading;

namespace Shuttle.Hopper;

public class DeferredMessageProcessor(IDeferredMessagePipeline deferredMessagePipeline, IDeferredMessageProcessorContext deferredMessageProcessorContext)
    : IProcessor
{
    private readonly IDeferredMessageProcessorContext _deferredMessageProcessorContext = Guard.AgainstNull(deferredMessageProcessorContext);

    public async ValueTask<bool> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        if (!_deferredMessageProcessorContext.ShouldCheckDeferredMessages)
        {
            return false;
        }

        Guard.AgainstNull(deferredMessagePipeline);

        deferredMessagePipeline.State.ResetReceivedMessage();
        deferredMessagePipeline.State.ResetDeferredMessageReturned();
        deferredMessagePipeline.State.SetTransportMessage(null);

        await deferredMessagePipeline.ExecuteAsync(cancellationToken).ConfigureAwait(false);

        return await _deferredMessageProcessorContext.GetResultAsync(deferredMessagePipeline.State, cancellationToken);
    }
}