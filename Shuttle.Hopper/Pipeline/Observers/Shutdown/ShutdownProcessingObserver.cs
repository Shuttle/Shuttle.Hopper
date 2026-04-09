using Shuttle.Contract;
using Shuttle.Pipelines;
using Shuttle.Reflection;

namespace Shuttle.Hopper;

public interface IShutdownProcessingObserver : IPipelineObserver<Stopping>;

public class ShutdownProcessingObserver(ITransportService transportService) : IShutdownProcessingObserver
{
    private readonly ITransportService _transportService = Guard.AgainstNull(transportService);

    public async Task ExecuteAsync(IPipelineContext<Stopping> pipelineContext, CancellationToken cancellationToken = default)
    {
        await _transportService.TryDisposeAsync();
    }
}