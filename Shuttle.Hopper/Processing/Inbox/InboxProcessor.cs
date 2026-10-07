using Shuttle.Contract;
using Shuttle.Threading;

namespace Shuttle.Hopper;

public class InboxProcessor(IInboxMessagePipeline inboxMessagePipeline) : IProcessor
{
    public static string GetServiceKey(string inboxName)
    {
        return $"InboxProcessor:{Guard.AgainstEmpty(inboxName).ToLowerInvariant()}";
    }

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