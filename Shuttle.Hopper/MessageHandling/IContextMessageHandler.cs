namespace Shuttle.Hopper;

public interface IContextMessageHandler<in T> where T : class
{
    Task ProcessMessageAsync(IHandlerContext<T> context, CancellationToken cancellationToken = default);
}