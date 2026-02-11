using Shuttle.Core.Contract;
using Shuttle.Core.Pipelines;
using Shuttle.Core.Threading;

namespace Shuttle.Hopper;

public class InboxProcessor(IPipelineFactory pipelineFactory) : IProcessor
{
    public async ValueTask<bool> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        var messagePipeline = await Guard.AgainstNull(pipelineFactory).GetPipelineAsync<InboxMessagePipeline>(cancellationToken);

        messagePipeline.State.SetTransportMessage(null);
        messagePipeline.State.ResetReceivedMessage();

        if (cancellationToken.IsCancellationRequested)
        {
            return false;
        }

        await messagePipeline.ExecuteAsync(cancellationToken).ConfigureAwait(false);

        return messagePipeline.State.HasReceivedMessage();
    }
}