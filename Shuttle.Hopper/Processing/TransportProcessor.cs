using Shuttle.Core.Contract;
using Shuttle.Core.Pipelines;
using Shuttle.Core.Threading;

namespace Shuttle.Hopper;

public abstract class TransportProcessor<TPipeline>(IPipelineFactory pipelineFactory) : IProcessor where TPipeline : IPipeline
{
    private readonly IPipelineFactory _pipelineFactory = Guard.AgainstNull(pipelineFactory);

    public async ValueTask<bool> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        var messagePipeline = await _pipelineFactory.GetPipelineAsync<TPipeline>(cancellationToken);

        messagePipeline.State.ResetWorkPerformed();

        await messagePipeline.ExecuteAsync(cancellationToken).ConfigureAwait(false);

        return messagePipeline.State.GetWorkPerformed();
    }
}