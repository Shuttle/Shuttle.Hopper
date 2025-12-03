namespace Shuttle.Hopper;

public interface ICreateTransport
{
    Task CreateAsync(CancellationToken cancellationToken = default);
}