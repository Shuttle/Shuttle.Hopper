using System.Collections.ObjectModel;

namespace Shuttle.Hopper;

public class MessageHandlerDelegateRegistry(IDictionary<Type, MessageHandlerDelegate> contextHandlerDelegates) : IMessageHandlerDelegateRegistry
{
    private readonly IReadOnlyDictionary<Type, MessageHandlerDelegate> _delegates = new ReadOnlyDictionary<Type, MessageHandlerDelegate>(contextHandlerDelegates);

    public bool TryGetValue(Type messageType, out MessageHandlerDelegate? handler) => _delegates.TryGetValue(messageType, out handler);
}
