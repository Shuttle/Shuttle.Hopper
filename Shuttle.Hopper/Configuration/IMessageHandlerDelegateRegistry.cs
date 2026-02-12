namespace Shuttle.Hopper;

public interface IMessageHandlerDelegateRegistry
{
    bool TryGetValue(Type messageType, out DirectMessageHandlerDelegate? handler);
}