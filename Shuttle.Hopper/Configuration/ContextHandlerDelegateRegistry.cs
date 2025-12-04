using System.Collections.ObjectModel;

namespace Shuttle.Hopper;

public class ContextHandlerDelegateRegistry(IDictionary<Type, ContextHandlerDelegate> contextHandlerDelegates) : IContextHandlerDelegateRegistry
{
    private readonly IReadOnlyDictionary<Type, ContextHandlerDelegate> _contextHandlerDelegates = new ReadOnlyDictionary<Type, ContextHandlerDelegate>(contextHandlerDelegates);

    public bool TryGetValue(Type messageType, out ContextHandlerDelegate? handler) => _contextHandlerDelegates.TryGetValue(messageType, out handler);
}
