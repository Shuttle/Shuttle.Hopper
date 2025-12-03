using Microsoft.Extensions.Hosting;
using Shuttle.Core.Contract;

namespace Shuttle.Hopper;

public class ServiceBusHostedService(IServiceBus serviceBus) : IHostedService
{
    private readonly IServiceBus _serviceBus = Guard.AgainstNull(serviceBus);

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await _serviceBus.StartAsync(cancellationToken);
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        await _serviceBus.StopAsync(cancellationToken);
    }
}