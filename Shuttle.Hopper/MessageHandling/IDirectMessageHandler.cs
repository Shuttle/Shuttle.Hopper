namespace Shuttle.Hopper;

public interface IDirectMessageHandler<in T> where T : class
{
    Task ProcessMessageAsync(T message, CancellationToken cancellationToken = default);
}