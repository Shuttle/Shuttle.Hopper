using Shuttle.Core.Contract;
using Shuttle.Core.Pipelines;

namespace Shuttle.Hopper;

public interface IReceiveDeferredMessageObserver : IPipelineObserver<ReceiveMessage>
{
}

public class ReceiveDeferredMessageObserver : IReceiveDeferredMessageObserver
{
    public async Task ExecuteAsync(IPipelineContext<ReceiveMessage> pipelineContext, CancellationToken cancellationToken = default)
    {
        var state = Guard.AgainstNull(pipelineContext).Pipeline.State;
        var transport = Guard.AgainstNull(state.GetDeferredTransport());

        var receivedMessage = await transport.ReceiveAsync(cancellationToken).ConfigureAwait(false);

        // Abort the pipeline if there is no message on the transport
        if (receivedMessage == null)
        {
            pipelineContext.Pipeline.Abort();
        }
        else
        {
            state.SetWorking();
            state.SetReceivedMessage(receivedMessage);
        }
    }
}