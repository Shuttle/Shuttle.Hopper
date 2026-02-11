using Shuttle.Core.Contract;
using Shuttle.Core.Pipelines;

namespace Shuttle.Hopper;

public interface IReceiveWorkMessageObserver : IPipelineObserver<ReceiveMessage>;

public class ReceiveWorkMessageObserver : IReceiveWorkMessageObserver
{
    public async Task ExecuteAsync(IPipelineContext<ReceiveMessage> pipelineContext, CancellationToken cancellationToken = default)
    {
        var state = Guard.AgainstNull(pipelineContext).Pipeline.State;
        var transport = Guard.AgainstNull(state.GetWorkTransport());

        var receivedMessage = await transport.ReceiveAsync(cancellationToken).ConfigureAwait(false);

        if (receivedMessage == null)
        {
            pipelineContext.Pipeline.Abort();

            return;
        }

        state.SetReceivedMessage(receivedMessage);
    }
}