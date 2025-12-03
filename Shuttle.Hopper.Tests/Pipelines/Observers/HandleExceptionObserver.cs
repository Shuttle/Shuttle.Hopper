using Shuttle.Core.Pipelines;

namespace Shuttle.Hopper.Tests;

public class HandleExceptionObserver : IPipelineObserver<OnPipelineException>
{
    public async Task ExecuteAsync(IPipelineContext<OnPipelineException> pipelineContext, CancellationToken cancellationToken = default)
    {
        pipelineContext.Pipeline.MarkExceptionHandled();

        await Task.CompletedTask;
    }
}