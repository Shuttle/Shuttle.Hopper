using Shuttle.Core.Contract;
using Shuttle.Core.Pipelines;
using Shuttle.Core.Threading;

namespace Shuttle.Hopper;

public class InboxProcessor(ServiceBusOptions serviceBusOptions, IThreadActivity threadActivity, IPipelineFactory pipelineFactory)
    : IProcessor
{
    private readonly IPipelineFactory _pipelineFactory = Guard.AgainstNull(pipelineFactory);
    private readonly ServiceBusOptions _serviceBusOptions = Guard.AgainstNull(serviceBusOptions);
    private readonly IThreadActivity _threadActivity = Guard.AgainstNull(threadActivity);

    public async Task ExecuteAsync(IProcessorThreadContext _, CancellationToken cancellationToken = default)
    {
        var messagePipeline = await _pipelineFactory.GetPipelineAsync<InboxMessagePipeline>(cancellationToken);

        try
        {
            messagePipeline.State.ResetWorking();
            messagePipeline.State.SetTransportMessage(null);
            messagePipeline.State.SetReceivedMessage(null);

            if (cancellationToken.IsCancellationRequested)
            {
                return;
            }

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