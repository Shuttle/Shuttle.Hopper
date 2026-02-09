using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Shuttle.Core.Contract;

namespace Shuttle.Hopper;

public class BusHostedService(IOptions<HopperOptions> hopperOptions, IServiceScopeFactory serviceScopeFactory) : IHostedService
{
    private readonly HopperOptions _hopperOptions = Guard.AgainstNull(Guard.AgainstNull(hopperOptions).Value);
    private IBusControl? _bus;
    private IServiceScope? _serviceScope;

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (_hopperOptions.SuppressBusHostedService)
        {
            return;
        }

        _serviceScope = Guard.AgainstNull(serviceScopeFactory).CreateScope();
        _bus = _serviceScope.ServiceProvider.GetRequiredService<IBusControl>();

        await _bus.StartAsync(cancellationToken);
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        if (_hopperOptions.SuppressBusHostedService)
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