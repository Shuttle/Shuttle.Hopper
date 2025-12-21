using System.Collections.ObjectModel;

namespace Shuttle.Hopper;

public class DirectMessageHandlerDelegateRegistry(IDictionary<Type, DirectMessageHandlerDelegate> messageHandlerDelegates) : IDirectMessageHandlerDelegateRegistry
{
    private readonly IReadOnlyDictionary<Type, DirectMessageHandlerDelegate> _delegates = new ReadOnlyDictionary<Type, DirectMessageHandlerDelegate>(messageHandlerDelegates);

    public bool TryGetValue(Type messageType, out DirectMessageHandlerDelegate? handler) => _delegates.TryGetValue(messageType, out handler);
}
