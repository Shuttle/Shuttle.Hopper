using Microsoft.Extensions.Options;
using Shuttle.Core.Pipelines;
using Shuttle.Core.TransactionScope;

namespace Shuttle.Hopper;

public interface IShutdownPipeline : IPipeline;

public class ShutdownPipeline : Pipeline, IShutdownPipeline
{
    public ShutdownPipeline(IOptions<PipelineOptions> pipelineOptions, IOptions<TransactionScopeOptions> transactionScopeOptions, ITransactionScopeFactory transactionScopeFactory, IServiceProvider serviceProvider)
        : base(pipelineOptions, transactionScopeOptions, transactionScopeFactory, serviceProvider)
    {
        AddStage("Shutdown")
            .WithEvent<Stopping>();

        AddStage("Final")
            .WithEvent<Stopped>();

        AddObserver<IShutdownProcessingObserver>();
    }
}