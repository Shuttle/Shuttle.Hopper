namespace Shuttle.Hopper;

public interface IMessageHandler<in T> where T : class
{
    Task ProcessMessageAsync(IHandlerContext<T> context);
}