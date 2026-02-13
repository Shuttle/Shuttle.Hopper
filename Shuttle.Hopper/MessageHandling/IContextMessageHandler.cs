namespace Shuttle.Hopper;

public interface IContextMessageHandler<in T> where T : class
{
    Task HandleAsync(IHandlerContext<T> context, CancellationToken cancellationToken = default);
}