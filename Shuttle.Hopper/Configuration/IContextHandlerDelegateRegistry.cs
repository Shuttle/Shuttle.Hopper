namespace Shuttle.Hopper;

public interface IContextHandlerDelegateRegistry
{
    bool TryGetValue(Type messageType, out ContextHandlerDelegate? handler);
}