namespace Shuttle.Hopper;

public interface IMessageHandlerDelegateProvider
{
    IDictionary<Type, MessageHandlerDelegate> Delegates { get; }
}