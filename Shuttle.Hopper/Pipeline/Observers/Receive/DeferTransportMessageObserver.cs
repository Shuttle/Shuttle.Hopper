using Microsoft.Extensions.Options;
using Shuttle.Core.Contract;
using Shuttle.Core.Pipelines;
using Shuttle.Core.Streams;

namespace Shuttle.Hopper;

public interface IDeferTransportMessageObserver : IPipelineObserver<OnAfterDeserializeTransportMessage>;

public class DeferTransportMessageObserver(IOptions<ServiceBusOptions> serviceBusOptions, IDeferredMessageProcessor deferredMessageProcessor) : IDeferTransportMessageObserver
{
    private readonly IDeferredMessageProcessor _deferredMessageProcessor = Guard.AgainstNull(deferredMessageProcessor);

    public async Task ExecuteAsync(IPipelineContext<OnAfterDeserializeTransportMessage> pipelineContext, CancellationToken cancellation = default)
    {
        var state = Guard.AgainstNull(pipelineContext).Pipeline.State;
        var transportMessage = Guard.AgainstNull(state.GetTransportMessage());
        var workTransport = Guard.AgainstNull(state.GetWorkTransport());

        if (!transportMessage.IsIgnoring() || workTransport.Type == TransportType.Stream)
        {
            return;
        }

        var receivedMessage = Guard.AgainstNull(state.GetReceivedMessage());
        var deferredTransport = state.GetDeferredTransport();

        await using (var stream = await receivedMessage.Stream.CopyAsync().ConfigureAwait(false))
        {
            if (deferredTransport == null)
            {
                await workTransport.SendAsync(transportMessage, stream, cancellation).ConfigureAwait(false);
            }
            else
            {
                await deferredTransport.SendAsync(transportMessage, stream, cancellation).ConfigureAwait(false);

                await _deferredMessageProcessor.MessageDeferredAsync(transportMessage.IgnoreTillDate).ConfigureAwait(false);
            }
        }

        await workTransport.AcknowledgeAsync(receivedMessage.AcknowledgementToken, cancellation).ConfigureAwait(false);

        await serviceBusOptions.Value.TransportMessageDeferred.InvokeAsync(new(transportMessage), cancellation);

        pipelineContext.Pipeline.Abort();
    }
}