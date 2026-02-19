using Shuttle.Core.Contract;
using Shuttle.Core.Threading;

namespace Shuttle.Hopper;

public class InboxProcessor(IInboxMessagePipeline inboxMessagePipeline) : IProcessor
{
    public async ValueTask<bool> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        Guard.AgainstNull(inboxMessagePipeline);

        inboxMessagePipeline.State.SetTransportMessage(null);
        inboxMessagePipeline.State.ResetReceivedMessage();

        if (cancellationToken.IsCancellationRequested)
        {
            return false;
        }

        await inboxMessagePipeline.ExecuteAsync(cancellationToken).ConfigureAwait(false);

        return inboxMessagePipeline.State.HasReceivedMessage();
    }
}