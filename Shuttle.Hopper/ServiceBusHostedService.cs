using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Shuttle.Core.Contract;

namespace Shuttle.Hopper;

public class ServiceBusHostedService(IOptions<HopperOptions> hopperOptions, IServiceScopeFactory serviceScopeFactory) : IHostedService
{
    private readonly HopperOptions _hopperOptions = Guard.AgainstNull(Guard.AgainstNull(hopperOptions).Value);
    private IServiceBus? _serviceBus;
    private IServiceScope? _serviceScope;

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (_hopperOptions.SuppressServiceBusHostedService)
        {
            return;
        }

        _serviceScope = Guard.AgainstNull(serviceScopeFactory).CreateScope();
        _serviceBus = _serviceScope.ServiceProvider.GetRequiredService<IServiceBus>();

        await _serviceBus.StartAsync(cancellationToken);
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        if (_hopperOptions.SuppressServiceBusHostedService)
        {
            return;
        }

        if (_serviceBus != null)
        {
            await _serviceBus.DisposeAsync();
        }

        _serviceScope?.Dispose();
    }
}