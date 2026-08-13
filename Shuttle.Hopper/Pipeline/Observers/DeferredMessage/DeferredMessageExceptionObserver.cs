using Shuttle.Contract;
using Shuttle.Pipelines;

namespace Shuttle.Hopper;

public interface IDeferredMessagePipelineFailedObserver : IPipelineObserver<PipelineFailed>;

/// <summary>
///     Handles any exception raised within the deferred message pipeline.  Without this the exception is re-thrown out
///     of `DeferredMessagePipeline.ExecuteAsync`, which means that `DeferredMessageProcessor` never obtains a result
///     from the `IDeferredMessageProcessorContext` and the processor thread neither backs off nor releases the deferred
///     message that is still in-flight.
/// </summary>
public class DeferredMessagePipelineFailedObserver : IDeferredMessagePipelineFailedObserver
{
    public async Task ExecuteAsync(IPipelineContext<PipelineFailed> pipelineContext, CancellationToken cancellationToken = default)
    {
        var state = Guard.AgainstNull(pipelineContext).Pipeline.State;

        try
        {
            if (pipelineContext.Pipeline.ExceptionHandled)
            {
                return;
            }

            var receivedMessage = state.GetReceivedMessage();
            var deferredTransport = state.GetReceivedTransport() ?? state.GetDeferredTransport();

            if (receivedMessage != null && deferredTransport != null)
            {
                await deferredTransport.ReleaseAsync(receivedMessage.AcknowledgementToken, pipelineContext.Pipeline, cancellationToken).ConfigureAwait(false);
            }

            // The message was returned to the deferred transport without having been evaluated, so the processor
            // context should treat this pass as though nothing was found; it will then apply the reset interval
            // instead of taking the message as the scan checkpoint.
            state.ResetReceivedMessage();
            state.ResetDeferredMessageReturned();
            state.SetTransportMessage(null);
        }
        finally
        {
            pipelineContext.Pipeline.MarkExceptionHandled();
            pipelineContext.Pipeline.Abort();
        }
    }
}
