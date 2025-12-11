using Shuttle.Core.Contract;
using Shuttle.Core.Pipelines;

namespace Shuttle.Hopper;

public interface IAcknowledgeMessageObserver : IPipelineObserver<MessageAcknowledged>;

public class AcknowledgeMessageObserver : IAcknowledgeMessageObserver
{
    public async Task ExecuteAsync(IPipelineContext<MessageAcknowledged> pipelineContext, CancellationToken cancellationToken = default)
    {
        var state = Guard.AgainstNull(pipelineContext).Pipeline.State;

        if (pipelineContext.Pipeline.Exception != null)
        {
            return;
        }

        var acknowledgementToken = Guard.AgainstNull(state.GetReceivedMessage()).AcknowledgementToken;

        await Guard.AgainstNull(state.GetWorkTransport()).AcknowledgeAsync(acknowledgementToken, cancellationToken).ConfigureAwait(false);
    }
}