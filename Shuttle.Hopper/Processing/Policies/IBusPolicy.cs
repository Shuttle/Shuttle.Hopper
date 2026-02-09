using Shuttle.Core.Pipelines;

namespace Shuttle.Hopper;

public interface IBusPolicy
{
    MessageFailureAction EvaluateMessageHandlingFailure(IPipelineContext<PipelineFailed> pipelineContext);
    MessageFailureAction EvaluateOutboxFailure(IPipelineContext<PipelineFailed> pipelineContext);
}