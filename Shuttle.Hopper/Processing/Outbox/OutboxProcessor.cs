using Shuttle.Contract;
using Shuttle.Threading;

namespace Shuttle.Hopper;

public class OutboxProcessor(IOutboxPipeline outboxPipeline) : IProcessor
{
    public async ValueTask<bool> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        Guard.AgainstNull(outboxPipeline);

        outboxPipeline.State.ResetReceivedMessage();

        await outboxPipeline.ExecuteAsync(cancellationToken).ConfigureAwait(false);

        return outboxPipeline.State.HasReceivedMessage();
    }
}