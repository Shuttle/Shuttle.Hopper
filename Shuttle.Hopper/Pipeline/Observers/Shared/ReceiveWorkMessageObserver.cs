using Shuttle.Contract;
using Shuttle.Pipelines;

namespace Shuttle.Hopper;

public interface IReceiveWorkMessageObserver : IPipelineObserver<ReceiveMessage>;

public class ReceiveWorkMessageObserver : IReceiveWorkMessageObserver
{
    public async Task ExecuteAsync(IPipelineContext<ReceiveMessage> pipelineContext, CancellationToken cancellationToken = default)
    {
        var state = Guard.AgainstNull(pipelineContext).Pipeline.State;
        var transport = Guard.AgainstNull(state.GetWorkTransport());

        var receivedMessage = await transport.ReceiveAsync(pipelineContext.Pipeline, cancellationToken).ConfigureAwait(false);

        if (receivedMessage == null)
        {
            pipelineContext.Pipeline.Abort();

            return;
        }

        state.SetReceivedMessage(receivedMessage);
    }
}