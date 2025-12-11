using Shuttle.Core.Contract;
using Shuttle.Core.Pipelines;
using Shuttle.Core.Reflection;
using Shuttle.Core.Serialization;

namespace Shuttle.Hopper;

public interface IOutboxExceptionObserver : IPipelineObserver<PipelineFailed>;

public class OutboxExceptionObserver(IServiceBusPolicy policy, ISerializer serializer) : IOutboxExceptionObserver
{
    private readonly IServiceBusPolicy _policy = Guard.AgainstNull(policy);
    private readonly ISerializer _serializer = Guard.AgainstNull(serializer);

    public async Task ExecuteAsync(IPipelineContext<PipelineFailed> pipelineContext, CancellationToken cancellationToken = default)
    {
        var state = pipelineContext.Pipeline.State;

        try
        {
            state.ResetWorking();

            if (pipelineContext.Pipeline.ExceptionHandled)
            {
                return;
            }

            try
            {
                var receivedMessage = state.GetReceivedMessage();
                var transportMessage = state.GetTransportMessage();
                var workTransport = Guard.AgainstNull(state.GetWorkTransport());
                var errorTransport = state.GetErrorTransport();

                if (transportMessage == null)
                {
                    if (receivedMessage == null)
                    {
                        return;
                    }

                    await workTransport.ReleaseAsync(receivedMessage.AcknowledgementToken, cancellationToken).ConfigureAwait(false);

                    return;
                }

                Guard.AgainstNull(receivedMessage);

                if (workTransport.Type == TransportType.Queue)
                {
                    var action = _policy.EvaluateOutboxFailure(pipelineContext);

                    transportMessage.RegisterFailure(Guard.AgainstNull(pipelineContext.Pipeline.Exception).AllMessages(), action.TimeSpanToIgnoreRetriedMessage);

                    if (action.Retry || errorTransport == null)
                    {
                        await workTransport.SendAsync(transportMessage, await _serializer.SerializeAsync(transportMessage, cancellationToken).ConfigureAwait(false), cancellationToken).ConfigureAwait(false);
                    }
                    else
                    {
                        await errorTransport.SendAsync(transportMessage, await _serializer.SerializeAsync(transportMessage, cancellationToken).ConfigureAwait(false), cancellationToken).ConfigureAwait(false);
                    }

                    await workTransport.AcknowledgeAsync(receivedMessage!.AcknowledgementToken, cancellationToken).ConfigureAwait(false);
                }
                else
                {
                    await workTransport.ReleaseAsync(receivedMessage!.AcknowledgementToken, cancellationToken).ConfigureAwait(false);
                }
            }
            finally
            {
                pipelineContext.Pipeline.MarkExceptionHandled();
            }
        }
        finally
        {
            pipelineContext.Pipeline.Abort();
        }
    }
}