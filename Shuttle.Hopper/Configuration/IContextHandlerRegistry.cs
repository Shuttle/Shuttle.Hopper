namespace Shuttle.Hopper;

public interface IContextHandlerRegistry
{
    bool TryGetValue(Type messageType, out ContextHandlerDelegate? handler);
}