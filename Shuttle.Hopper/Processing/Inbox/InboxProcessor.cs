using Shuttle.Core.Contract;
using Shuttle.Core.Pipelines;
using Shuttle.Core.Threading;

namespace Shuttle.Hopper;

public class InboxProcessor(IPipelineFactory pipelineFactory) : IProcessor
{
    private readonly IPipelineFactory _pipelineFactory = Guard.AgainstNull(pipelineFactory);

    public async ValueTask<bool> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        var messagePipeline = await _pipelineFactory.GetPipelineAsync<InboxMessagePipeline>(cancellationToken);

        messagePipeline.State.ResetWorkPerformed();
        messagePipeline.State.SetTransportMessage(null);
        messagePipeline.State.SetReceivedMessage(null);

        if (cancellationToken.IsCancellationRequested)
        {
            return false;
        }

        await messagePipeline.ExecuteAsync(cancellationToken).ConfigureAwait(false);

        return messagePipeline.State.GetWorkPerformed();
    }
}