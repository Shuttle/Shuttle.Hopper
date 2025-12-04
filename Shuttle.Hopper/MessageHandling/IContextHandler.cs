namespace Shuttle.Hopper;

public interface IContextHandler<in T> where T : class
{
    Task ProcessMessageAsync(IHandlerContext<T> context, CancellationToken cancellationToken = default);
}