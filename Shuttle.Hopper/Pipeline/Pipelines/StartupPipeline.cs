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
            .WithEvent<OnStarting>()
            .WithEvent<OnCreatePhysicalTransports>()
            .WithEvent<OnAfterCreatePhysicalTransports>()
            .WithEvent<OnConfigureThreadPools>()
            .WithEvent<OnAfterConfigureThreadPools>()
            .WithEvent<OnStartThreadPools>()
            .WithEvent<OnAfterStartThreadPools>();

        AddStage("Final")
            .WithEvent<OnStarted>();

        AddObserver(Guard.AgainstNull(startupProcessingObserver));
    }
}