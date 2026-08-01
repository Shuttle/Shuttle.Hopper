using System.Transactions;
using Shuttle.Contract;
using Shuttle.Pipelines;
using Shuttle.Reflection;
using Shuttle.Serialization;

namespace Shuttle.Hopper;

public interface IReceivePipelineFailedObserver : IPipelineObserver<PipelineFailed>;

public class ReceivePipelineFailedObserver(IBusPolicy policy, ISerializer serializer) : IReceivePipelineFailedObserver
{
    private readonly IBusPolicy _policy = Guard.AgainstNull(policy);
    private readonly ISerializer _serializer = Guard.AgainstNull(serializer);

    public async Task ExecuteAsync(IPipelineContext<PipelineFailed> pipelineContext, CancellationToken cancellationToken = default)
    {
        Guard.AgainstNull(pipelineContext);

        var state = pipelineContext.Pipeline.State;

        try
        {
            using var tx = new TransactionScope(TransactionScopeOption.RequiresNew, TransactionScopeAsyncFlowOption.Enabled);

            if (pipelineContext.Pipeline.ExceptionHandled)
            {
                return;
            }

            try
            {
                var transportMessage = state.GetTransportMessage();
                var receivedMessage = state.GetReceivedMessage();
                var workTransport = Guard.AgainstNull(state.GetWorkTransport());

                if (transportMessage == null)
                {
                    if (receivedMessage == null)
                    {
                        return;
                    }

                    await workTransport.ReleaseAsync(receivedMessage.AcknowledgementToken, pipelineContext.Pipeline, cancellationToken).ConfigureAwait(false);

                    return;
                }

                var action = _policy.EvaluateMessageHandlingFailure(pipelineContext);

                var errorTransport = state.GetErrorTransport();
                var exception = Guard.AgainstNull(pipelineContext.Pipeline.Exception);

                transportMessage.RegisterFailure(exception.AllMessages(), action.TimeSpanToIgnoreRetriedMessage);

                var retry = workTransport.Type == TransportType.Queue
                            && !exception.Contains<UnrecoverableHandlerException>()
                            && action.Retry;

                var poison = errorTransport != null && !retry;

                Guard.AgainstNull(receivedMessage);

                if (retry || poison)
                {
                    await using (var stream = await _serializer.SerializeAsync(transportMessage, cancellationToken).ConfigureAwait(false))
                    {
                        if (retry)
                        {
                            await workTransport.SendAsync(stream, pipelineContext.Pipeline, cancellationToken).ConfigureAwait(false);
                        }

                        if (poison)
                        {
                            await errorTransport!.SendAsync(stream, pipelineContext.Pipeline, cancellationToken).ConfigureAwait(false);
                        }
                    }

                    await workTransport.AcknowledgeAsync(receivedMessage!.AcknowledgementToken, pipelineContext.Pipeline, cancellationToken).ConfigureAwait(false);
                }
                else
                {
                    await workTransport.ReleaseAsync(receivedMessage!.AcknowledgementToken, pipelineContext.Pipeline, cancellationToken).ConfigureAwait(false);
                }
            }
            finally
            {
                pipelineContext.Pipeline.MarkExceptionHandled();

                tx.Complete();
            }
        }
        finally
        {
            pipelineContext.Pipeline.Abort();
        }
    }
}