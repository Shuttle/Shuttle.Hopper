namespace Shuttle.Hopper;

public class MessageHandlerDelegateProvider(IDictionary<Type, MessageHandlerDelegate> delegates) : IMessageHandlerDelegateProvider
{
    public IDictionary<Type, MessageHandlerDelegate> Delegates { get; } = delegates;
}