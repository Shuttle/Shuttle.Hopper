using Shuttle.Core.Pipelines;

namespace Shuttle.Hopper.Tests;

public class HandleExceptionObserver : IPipelineObserver<PipelineFailed>
{
    public Task ExecuteAsync(IPipelineContext<PipelineFailed> pipelineContext, CancellationToken cancellationToken = default)
    {
        pipelineContext.Pipeline.MarkExceptionHandled();

        return Task.CompletedTask;
    }
}