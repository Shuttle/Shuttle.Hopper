namespace Shuttle.Hopper;

public interface IPurgeTransport
{
    Task PurgeAsync(CancellationToken cancellationToken = default);
}