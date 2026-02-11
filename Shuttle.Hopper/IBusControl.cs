namespace Shuttle.Hopper;

public interface IBusControl : IDisposable, IAsyncDisposable
{
    bool Started { get; }
    Task<IBusControl> StartAsync(CancellationToken cancellationToken = default);
    Task StopAsync(CancellationToken cancellationToken = default);
}