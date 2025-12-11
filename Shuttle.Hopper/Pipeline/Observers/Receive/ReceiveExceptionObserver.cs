using System.Transactions;
using Shuttle.Core.Contract;
using Shuttle.Core.Pipelines;
using Shuttle.Core.Reflection;
using Shuttle.Core.Serialization;

namespace Shuttle.Hopper;

public interface IReceivePipelineFailedObserver : IPipelineObserver<PipelineFailed>;

public class ReceivePipelineFailedObserver(IServiceBusPolicy policy, ISerializer serializer) : IReceivePipelineFailedObserver
{
    private readonly IServiceBusPolicy _policy = Guard.AgainstNull(policy);
    private readonly ISerializer _serializer = Guard.AgainstNull(serializer);

    public async Task ExecuteAsync(IPipelineContext<PipelineFailed> pipelineContext, CancellationToken cancellationToken = default)
    {
        Guard.AgainstNull(pipelineContext);

        var state = pipelineContext.Pipeline.State;

        try
        {
            using var tx = new TransactionScope(TransactionScopeOption.RequiresNew, TransactionScopeAsyncFlowOption.Enabled);

            state.ResetWorking();

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

                    await workTransport.ReleaseAsync(receivedMessage.AcknowledgementToken, cancellationToken).ConfigureAwait(false);

                    return;
                }

                var action = _policy.EvaluateMessageHandlingFailure(pipelineContext);

                var errorTransport = state.GetErrorTransport();
                var handlerContext = state.GetHandlerContext() as IHandlerContext;
                var exception = Guard.AgainstNull(pipelineContext.Pipeline.Exception);

                transportMessage.RegisterFailure(exception.AllMessages(), action.TimeSpanToIgnoreRetriedMessage);

                var retry = workTransport.Type == TransportType.Queue;

                retry = retry && !exception.Contains<UnrecoverableHandlerException>();
                retry = retry && action.Retry;

                if (retry && handlerContext != null)
                {
                    retry =
                        handlerContext.ExceptionHandling == ExceptionHandling.Retry ||
                        handlerContext.ExceptionHandling == ExceptionHandling.Default;
                }

                var poison = errorTransport != null;

                poison = poison && !retry;

                if (poison && handlerContext != null)
                {
                    poison =
                        handlerContext.ExceptionHandling == ExceptionHandling.Poison ||
                        handlerContext.ExceptionHandling == ExceptionHandling.Default;
                }

                Guard.AgainstNull(receivedMessage);

                if (retry || poison)
                {
                    await using (var stream = await _serializer.SerializeAsync(transportMessage, cancellationToken).ConfigureAwait(false))
                    {
                        if (retry)
                        {
                            await workTransport.SendAsync(transportMessage, stream, cancellationToken).ConfigureAwait(false);
                        }

                        if (poison)
                        {
                            await errorTransport!.SendAsync(transportMessage, stream, cancellationToken).ConfigureAwait(false);
                        }
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

                tx.Complete();
            }
        }
        finally
        {
            pipelineContext.Pipeline.Abort();
        }
    }
}