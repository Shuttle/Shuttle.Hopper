using Shuttle.Core.Pipelines;

namespace Shuttle.Hopper;

public interface IServiceBusPolicy
{
    MessageFailureAction EvaluateMessageHandlingFailure(IPipelineContext<PipelineFailed> pipelineContext);
    MessageFailureAction EvaluateOutboxFailure(IPipelineContext<PipelineFailed> pipelineContext);
}