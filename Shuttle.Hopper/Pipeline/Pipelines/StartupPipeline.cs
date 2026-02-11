using Microsoft.Extensions.Options;
using Shuttle.Core.Pipelines;
using Shuttle.Core.TransactionScope;

namespace Shuttle.Hopper;

public class StartupPipeline : Pipeline
{
    public StartupPipeline(IOptions<PipelineOptions> pipelineOptions, IOptions<TransactionScopeOptions> transactionScopeOptions, ITransactionScopeFactory transactionScopeFactory, IServiceProvider serviceProvider)
        : base(pipelineOptions, transactionScopeOptions, transactionScopeFactory, serviceProvider)
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