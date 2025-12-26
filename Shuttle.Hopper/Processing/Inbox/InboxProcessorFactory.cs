using Shuttle.Core.Contract;
using Shuttle.Core.Pipelines;
using Shuttle.Core.Threading;

namespace Shuttle.Hopper;

public interface IInboxProcessorFactory : IProcessorFactory;

public class InboxProcessorFactory(ServiceBusOptions serviceBusOptions, IPipelineFactory pipelineFactory)
    : IInboxProcessorFactory
{
    private readonly IPipelineFactory _pipelineFactory = Guard.AgainstNull(pipelineFactory);
    private readonly ServiceBusOptions _serviceBusOptions = Guard.AgainstNull(serviceBusOptions);

    public async Task<IProcessor> CreateAsync(CancellationToken cancellationToken = default)
    {
        return await Task.FromResult(new InboxProcessor(_serviceBusOptions, new ThreadActivity(_serviceBusOptions.Inbox.IdleDurations), _pipelineFactory));
    }
}