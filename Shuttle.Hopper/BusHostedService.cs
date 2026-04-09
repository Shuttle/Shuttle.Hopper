using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Shuttle.Contract;

namespace Shuttle.Hopper;

public class BusHostedService(IOptions<HopperOptions> hopperOptions, IServiceScopeFactory serviceScopeFactory) : IHostedService
{
    private IBusControl? _bus;
    private IServiceScope? _serviceScope;

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (!hopperOptions.Value.AutoStart)
        {
            return;
        }

        _serviceScope = Guard.AgainstNull(serviceScopeFactory).CreateScope();
        _bus = _serviceScope.ServiceProvider.GetRequiredService<IBusControl>();

        await _bus.StartAsync(cancellationToken);
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        if (!hopperOptions.Value.AutoStart)
        {
            return;
        }

        if (_bus != null)
        {
            await _bus.DisposeAsync();
        }

        _serviceScope?.Dispose();
    }
}