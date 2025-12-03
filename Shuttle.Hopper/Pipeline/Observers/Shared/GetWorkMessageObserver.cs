using Shuttle.Core.Contract;
using Shuttle.Core.Pipelines;

namespace Shuttle.Hopper;

public interface IGetWorkMessageObserver : IPipelineObserver<OnGetMessage>;

public class GetWorkMessageObserver : IGetWorkMessageObserver
{
    public async Task ExecuteAsync(IPipelineContext<OnGetMessage> pipelineContext, CancellationToken cancellationToken = default)
    {
        var state = Guard.AgainstNull(pipelineContext).Pipeline.State;
        var transport = Guard.AgainstNull(state.GetWorkTransport());

        var receivedMessage = await transport.ReceiveAsync(cancellationToken).ConfigureAwait(false);

        if (receivedMessage == null)
        {
            pipelineContext.Pipeline.Abort();

            return;
        }

        state.SetWorking();
        state.SetReceivedMessage(receivedMessage);
    }
}