using Microsoft.Extensions.Options;
using Shuttle.Core.Contract;
using Shuttle.Core.Pipelines;
using Shuttle.Core.TransactionScope;

namespace Shuttle.Hopper;

public class ShutdownPipeline : Pipeline
{
    public ShutdownPipeline(IOptions<PipelineOptions> pipelineOptions, IOptions<TransactionScopeOptions> transactionScopeOptions, ITransactionScopeFactory transactionScopeFactory, IServiceProvider serviceProvider, IShutdownProcessingObserver shutdownProcessingObserver)
        : base(pipelineOptions, transactionScopeOptions, transactionScopeFactory, serviceProvider)
    {
        AddStage("Shutdown")
            .WithEvent<Stopping>();

        AddStage("Final")
            .WithEvent<Stopped>();

        AddObserver(Guard.AgainstNull(shutdownProcessingObserver));
    }
}