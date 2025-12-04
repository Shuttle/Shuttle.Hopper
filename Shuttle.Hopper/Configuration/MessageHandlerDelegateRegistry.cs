using System.Collections.ObjectModel;

namespace Shuttle.Hopper;

public class MessageHandlerDelegateRegistry(IDictionary<Type, MessageHandlerDelegate> messageHandlerDelegates) : IMessageHandlerDelegateRegistry
{
    private readonly IReadOnlyDictionary<Type, MessageHandlerDelegate> _messageHandlerDelegates = new ReadOnlyDictionary<Type, MessageHandlerDelegate>(messageHandlerDelegates);

    public bool TryGetValue(Type messageType, out MessageHandlerDelegate? handler) => _messageHandlerDelegates.TryGetValue(messageType, out handler);
}
