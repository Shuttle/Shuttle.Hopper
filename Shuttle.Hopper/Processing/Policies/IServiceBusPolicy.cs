using Shuttle.Core.Pipelines;

namespace Shuttle.Hopper;

public interface IServiceBusPolicy
{
    MessageFailureAction EvaluateMessageHandlingFailure(IPipelineContext<OnPipelineException> pipelineContext);
    MessageFailureAction EvaluateOutboxFailure(IPipelineContext<OnPipelineException> pipelineContext);
}