namespace Shuttle.Hopper;

public interface IMessageHandler<in T> where T : class
{
    Task ProcessMessageAsync(T message, CancellationToken cancellationToken = default);
}