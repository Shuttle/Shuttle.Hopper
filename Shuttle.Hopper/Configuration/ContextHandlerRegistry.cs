using System.Collections.ObjectModel;

namespace Shuttle.Hopper;

public class ContextHandlerRegistry(IDictionary<Type, ContextHandlerDelegate> delegates) : IContextHandlerRegistry
{
    private readonly IReadOnlyDictionary<Type, ContextHandlerDelegate> _delegates = new ReadOnlyDictionary<Type, ContextHandlerDelegate>(delegates);

    public bool TryGetValue(Type messageType, out ContextHandlerDelegate? handler) => _delegates.TryGetValue(messageType, out handler);
}
