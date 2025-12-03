using Microsoft.Extensions.Options;
using Shuttle.Core.Contract;
using Shuttle.Core.Pipelines;

namespace Shuttle.Hopper;

public interface IProcessDeferredMessageObserver : IPipelineObserver<OnProcessDeferredMessage>;

public class ProcessDeferredMessageObserver(IOptions<ServiceBusOptions> serviceBusOptions) : IProcessDeferredMessageObserver
{
    public async Task ExecuteAsync(IPipelineContext<OnProcessDeferredMessage> pipelineContext, CancellationToken cancellation = default)
    {
        var state = Guard.AgainstNull(pipelineContext).Pipeline.State;
        var transportMessage = Guard.AgainstNull(state.GetTransportMessage());
        var receivedMessage = Guard.AgainstNull(state.GetReceivedMessage());
        var workTransport = Guard.AgainstNull(state.GetWorkTransport());
        var deferredTransport = Guard.AgainstNull(state.GetDeferredTransport());

        if (transportMessage.IsIgnoring())
        {
            await deferredTransport.ReleaseAsync(receivedMessage.AcknowledgementToken, cancellation).ConfigureAwait(false);

            state.SetDeferredMessageReturned(false);

            return;
        }

        await workTransport.SendAsync(transportMessage, receivedMessage.Stream, cancellation).ConfigureAwait(false);
        await deferredTransport.AcknowledgeAsync(receivedMessage.AcknowledgementToken, cancellation).ConfigureAwait(false);

        state.SetDeferredMessageReturned(true);

        await serviceBusOptions.Value.MessageReturned.InvokeAsync(new(transportMessage, receivedMessage), cancellation);
    }
}