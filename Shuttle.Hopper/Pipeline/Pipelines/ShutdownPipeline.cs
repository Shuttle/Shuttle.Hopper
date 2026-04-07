using Microsoft.Extensions.Options;
using Shuttle.Core.Pipelines;

namespace Shuttle.Hopper;

public interface IShutdownPipeline : IPipeline;

public class ShutdownPipeline : Pipeline, IShutdownPipeline
{
    public ShutdownPipeline(IOptions<PipelineOptions> pipelineOptions, IServiceProvider serviceProvider)
        : base(pipelineOptions, serviceProvider)
    {
        AddStage("Shutdown")
            .WithEvent<Stopping>();

        AddStage("Final")
            .WithEvent<Stopped>();

        AddObserver<IShutdownProcessingObserver>();
    }
}