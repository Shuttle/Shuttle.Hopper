using System.Reflection.Emit;

namespace Shuttle.Hopper;

internal class HandlerContextConstructorInvoker
{
    private static readonly Type HandlerContextType = typeof(HandlerContext<>);

    private readonly ConstructorInvokeHandler _constructorInvoker;

    public HandlerContextConstructorInvoker(Type messageType)
    {
        var dynamicMethod = new DynamicMethod(string.Empty, typeof(object),
        [
            typeof(IMessageSender),
            typeof(TransportMessage),
            typeof(object)
        ], HandlerContextType.Module);

        var il = dynamicMethod.GetILGenerator();

        il.Emit(OpCodes.Ldarg_0);
        il.Emit(OpCodes.Ldarg_1);
        il.Emit(OpCodes.Ldarg_2);

        var contextType = HandlerContextType.MakeGenericType(messageType);
        var constructorInfo = contextType.GetConstructor(
        [
            typeof(IMessageSender),
            typeof(TransportMessage),
            messageType
        ]);

        if (constructorInfo == null)
        {
            throw new MessageHandlerInvokerException(string.Format(Resources.HandlerContextConstructorMissingException, contextType.FullName));
        }

        il.Emit(OpCodes.Newobj, constructorInfo);
        il.Emit(OpCodes.Ret);

        _constructorInvoker = (ConstructorInvokeHandler)dynamicMethod.CreateDelegate(typeof(ConstructorInvokeHandler));
    }

    public object CreateHandlerContext(IMessageSender messageSender, TransportMessage transportMessage, object message)
    {
        return _constructorInvoker(messageSender, transportMessage, message);
    }

    private delegate object ConstructorInvokeHandler(IMessageSender messageSender, TransportMessage transportMessage, object message);
}