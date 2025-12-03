using Shuttle.Core.Contract;
using Shuttle.Core.Pipelines;
using Shuttle.Core.Threading;

namespace Shuttle.Hopper;

public abstract class TransportProcessor<TPipeline>(ServiceBusOptions serviceBusOptions, IThreadActivity threadActivity, IPipelineFactory pipelineFactory)
    : IProcessor where TPipeline : IPipeline
{
    private readonly IPipelineFactory _pipelineFactory = Guard.AgainstNull(pipelineFactory);
    private readonly ServiceBusOptions _serviceBusOptions = Guard.AgainstNull(serviceBusOptions);
    private readonly IThreadActivity _threadActivity = Guard.AgainstNull(threadActivity);

    public async Task ExecuteAsync(IProcessorThreadContext _, CancellationToken cancellationToken = default)
    {
        var messagePipeline = await _pipelineFactory.GetPipelineAsync<TPipeline>(cancellationToken);

        try
        {
            messagePipeline.State.ResetWorking();

            await messagePipeline.ExecuteAsync(cancellationToken).ConfigureAwait(false);

            if (messagePipeline.State.GetWorking())
            {
                _threadActivity.Working();

                await _serviceBusOptions.ThreadWorking.InvokeAsync(new(messagePipeline), cancellationToken);
            }
            else
            {
                await _serviceBusOptions.ThreadWaiting.InvokeAsync(new(messagePipeline), cancellationToken);

                await _threadActivity.WaitingAsync(cancellationToken).ConfigureAwait(false);
            }
        }
        finally
        {
            await _pipelineFactory.ReleasePipelineAsync(messagePipeline, cancellationToken);
        }
    }
}