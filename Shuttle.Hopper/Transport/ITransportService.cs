namespace Shuttle.Hopper;

public interface ITransportService : IDisposable, IAsyncDisposable
{
    ValueTask<bool> ContainsAsync(Uri uri, CancellationToken cancellationToken = default);
    Task<ITransport?> FindAsync(Uri uri, CancellationToken cancellationToken = default);
    Task<ITransport> GetAsync(Uri uri, CancellationToken cancellationToken = default);
}