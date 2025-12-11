using Microsoft.Extensions.Options;
using Shuttle.Core.Contract;
using Shuttle.Core.Pipelines;

namespace Shuttle.Hopper;

public class StartupPipeline : Pipeline
{
    public StartupPipeline(IPipelineDependencies pipelineDependencies, IStartupProcessingObserver startupProcessingObserver)
        : base(pipelineDependencies)
    {
        AddStage("Start")
            .WithEvent<Starting>()
            .WithEvent<CreatePhysicalTransports>()
            .WithEvent<PhysicalTransportsCreated>()
            .WithEvent<ConfigureThreadPools>()
            .WithEvent<ThreadPoolsConfigured>()
            .WithEvent<StartThreadPools>()
            .WithEvent<ThreadPoolsStarted>();

        AddStage("Final")
            .WithEvent<Started>();

        AddObserver(Guard.AgainstNull(startupProcessingObserver));
    }
}