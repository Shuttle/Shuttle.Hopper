using Microsoft.Extensions.Options;
using Shuttle.Contract;
using Shuttle.Pipelines;

namespace Shuttle.Hopper;

public interface IProcessDeferredMessageObserver : IPipelineObserver<ProcessDeferredMessage>;

public class ProcessDeferredMessageObserver(IOptions<HopperOptions> hopperOptions) : IProcessDeferredMessageObserver
{
    public async Task ExecuteAsync(IPipelineContext<ProcessDeferredMessage> pipelineContext, CancellationToken cancellationToken = default)
    {
        var state = Guard.AgainstNull(pipelineContext).Pipeline.State;
        var transportMessage = Guard.AgainstNull(state.GetTransportMessage());
        var receivedMessage = Guard.AgainstNull(state.GetReceivedMessage());
        var workTransport = Guard.AgainstNull(state.GetWorkTransport());
        var deferredTransport = Guard.AgainstNull(state.GetDeferredTransport());

        if (transportMessage.IsIgnoring())
        {
            await deferredTransport.ReleaseAsync(receivedMessage.AcknowledgementToken, pipelineContext.Pipeline, cancellationToken).ConfigureAwait(false);

            state.ResetDeferredMessageReturned();

            return;
        }

        await workTransport.SendAsync(receivedMessage.Stream, pipelineContext.Pipeline, cancellationToken).ConfigureAwait(false);
        await deferredTransport.AcknowledgeAsync(receivedMessage.AcknowledgementToken, pipelineContext.Pipeline, cancellationToken).ConfigureAwait(false);

        state.DeferredMessageReturned();

        await hopperOptions.Value.DeferredMessageReturned.InvokeAsync(new(pipelineContext.Pipeline), cancellationToken);
    }
}