using Microsoft.Extensions.Options;
using Shuttle.Core.Pipelines;

namespace Shuttle.Hopper;

public interface IShutdownPipeline : IPipeline;

public class ShutdownPipeline : Pipeline, IShutdownPipeline
{
    public ShutdownPipeline(IOptions<PipelineOptions> pipelineOptions, IPipelineState pipelineState, IServiceProvider serviceProvider)
        : base(pipelineOptions, pipelineState, serviceProvider)
    {
        AddStage("Shutdown")
            .WithEvent<Stopping>();

        AddStage("Final")
            .WithEvent<Stopped>();

        AddObserver<IShutdownProcessingObserver>();
    }
}