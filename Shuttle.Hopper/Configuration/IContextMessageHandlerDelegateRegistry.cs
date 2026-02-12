namespace Shuttle.Hopper;

public interface IContextMessageHandlerDelegateRegistry
{
    bool TryGetValue(Type messageType, out MessageHandlerDelegate? handler);
}