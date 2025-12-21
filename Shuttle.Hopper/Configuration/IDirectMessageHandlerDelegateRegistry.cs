namespace Shuttle.Hopper;

public interface IDirectMessageHandlerDelegateRegistry
{
    bool TryGetValue(Type messageType, out DirectMessageHandlerDelegate? handler);
}