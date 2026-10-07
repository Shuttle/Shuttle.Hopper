using Shuttle.Contract;
using Shuttle.Threading;

namespace Shuttle.Hopper;

public class DeferredMessageProcessor(IDeferredMessagePipeline deferredMessagePipeline, IDeferredMessageProcessorContext deferredMessageProcessorContext)
    : IProcessor
{
    private readonly IDeferredMessageProcessorContext _deferredMessageProcessorContext = Guard.AgainstNull(deferredMessageProcessorContext);

    public static string GetServiceKey(string inboxName)
    {
        return $"DeferredMessageProcessor:{Guard.AgainstEmpty(inboxName).ToLowerInvariant()}";
    }

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