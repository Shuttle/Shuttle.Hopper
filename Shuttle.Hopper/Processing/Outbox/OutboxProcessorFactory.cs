using Shuttle.Core.Contract;
using Shuttle.Core.Pipelines;
using Shuttle.Core.Threading;

namespace Shuttle.Hopper;

public class OutboxProcessorFactory(ServiceBusOptions serviceBusOptions, IPipelineFactory pipelineFactory)
    : IProcessorFactory
{
    private readonly IPipelineFactory _pipelineFactory = Guard.AgainstNull(pipelineFactory);
    private readonly ServiceBusOptions _serviceBusOptions = Guard.AgainstNull(serviceBusOptions);

    public async Task<IProcessor> CreateAsync(CancellationToken cancellationToken = default)
    {
        return await Task.FromResult(new OutboxProcessor(serviceBusOptions, new ThreadActivity(_serviceBusOptions.Outbox!.DurationToSleepWhenIdle), _pipelineFactory));
    }
}