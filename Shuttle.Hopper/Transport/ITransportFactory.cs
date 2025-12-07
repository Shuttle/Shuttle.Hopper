namespace Shuttle.Hopper;

public interface ITransportFactory
{
    string Scheme { get; }
    Task<ITransport> CreateAsync(Uri uri, CancellationToken cancellationToken = default);
}