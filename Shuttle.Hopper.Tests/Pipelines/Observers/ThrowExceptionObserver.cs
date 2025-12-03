using Shuttle.Core.Pipelines;

namespace Shuttle.Hopper.Tests;

public class ThrowExceptionObserver : IPipelineObserver<OnException>
{
    public async Task ExecuteAsync(IPipelineContext<OnException> pipelineContext, CancellationToken cancellationToken = default)
    {
        await Task.CompletedTask;

        throw new(string.Empty, new UnrecoverableHandlerException());
    }

    public void Execute(OnException pipelineEvent)
    {
        throw new(string.Empty, new UnrecoverableHandlerException());
    }
}