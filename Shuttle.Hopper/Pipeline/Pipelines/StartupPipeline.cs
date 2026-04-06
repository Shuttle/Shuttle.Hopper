using Microsoft.Extensions.Options;
using Shuttle.Core.Pipelines;

namespace Shuttle.Hopper;

public interface IStartupPipeline : IPipeline;

public class StartupPipeline : Pipeline, IStartupPipeline
{
    public StartupPipeline(IOptions<PipelineOptions> pipelineOptions, IPipelineState pipelineState, IServiceProvider serviceProvider)
        : base(pipelineOptions, pipelineState, serviceProvider)
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

        AddObserver<IStartupProcessingObserver>();
    }
}