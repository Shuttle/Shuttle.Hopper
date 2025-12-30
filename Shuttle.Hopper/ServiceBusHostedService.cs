using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Shuttle.Core.Contract;

namespace Shuttle.Hopper;

public class ServiceBusHostedService(IOptions<HopperOptions> hopperOptions, IServiceBus serviceBus) : IHostedService
{
    private readonly HopperOptions _hopperOptions = Guard.AgainstNull(Guard.AgainstNull(hopperOptions).Value);
    private readonly IServiceBus _serviceBus = Guard.AgainstNull(serviceBus);

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (_hopperOptions.SuppressServiceBusHostedService)
        {
            return;
        }

        await _serviceBus.StartAsync(cancellationToken);
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        if (_hopperOptions.SuppressServiceBusHostedService)
        {
            return;
        }

        await _serviceBus.StopAsync(cancellationToken);
    }
}