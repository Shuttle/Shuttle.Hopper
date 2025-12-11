using Microsoft.Extensions.Options;
using Shuttle.Core.Contract;
using Shuttle.Core.Pipelines;

namespace Shuttle.Hopper;

public class ShutdownPipeline : Pipeline
{
    public ShutdownPipeline(IPipelineDependencies pipelineDependencies, IShutdownProcessingObserver shutdownProcessingObserver)
        : base(pipelineDependencies)
    {
        AddStage("Shutdown")
            .WithEvent<OnStopping>();

        AddStage("Final")
            .WithEvent<OnStopped>();

        AddObserver(Guard.AgainstNull(shutdownProcessingObserver));
    }
}