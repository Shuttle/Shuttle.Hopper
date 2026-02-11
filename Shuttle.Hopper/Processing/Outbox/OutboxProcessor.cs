using Shuttle.Core.Contract;
using Shuttle.Core.Pipelines;
using Shuttle.Core.Threading;

namespace Shuttle.Hopper;

public class OutboxProcessor(IPipelineFactory pipelineFactory) : IProcessor
{
    private readonly IPipelineFactory _pipelineFactory = Guard.AgainstNull(pipelineFactory);

    public async ValueTask<bool> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        var messagePipeline = await _pipelineFactory.GetPipelineAsync<OutboxPipeline>(cancellationToken);

        messagePipeline.State.ResetReceivedMessage();

        await messagePipeline.ExecuteAsync(cancellationToken).ConfigureAwait(false);

        return messagePipeline.State.HasReceivedMessage();
    }
}