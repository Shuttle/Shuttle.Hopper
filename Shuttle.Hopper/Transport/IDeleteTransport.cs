namespace Shuttle.Hopper;

public interface IDeleteTransport
{
    Task DeleteAsync(CancellationToken cancellationToken = default);
}