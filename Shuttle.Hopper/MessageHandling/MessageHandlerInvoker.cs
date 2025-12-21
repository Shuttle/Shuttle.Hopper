using Shuttle.Core.Contract;
using Shuttle.Core.Pipelines;

namespace Shuttle.Hopper;

public class MessageHandlerInvoker(IServiceProvider serviceProvider, IMessageSender messageSender, IMessageHandlerDelegateRegistry messageHandlerDelegateRegistry, IDirectMessageHandlerDelegateRegistry directMessageHandlerDelegateRegistry)
    : IMessageHandlerInvoker
{
    private readonly IServiceProvider _serviceProvider = Guard.AgainstNull(serviceProvider);
    private readonly IMessageSender _messageSender = Guard.AgainstNull(messageSender);
    private readonly IMessageHandlerDelegateRegistry _messageHandlerDelegateRegistry = Guard.AgainstNull(messageHandlerDelegateRegistry);
    private readonly IDirectMessageHandlerDelegateRegistry _directMessageHandlerDelegateRegistry = Guard.AgainstNull(directMessageHandlerDelegateRegistry);
    private static readonly Type MessageHandlerType = typeof(IMessageHandler<>);
    private static readonly Type DirectMessageHandlerType = typeof(IDirectMessageHandler<>);
    private readonly Dictionary<Type, HandlerContextConstructorInvoker> _handlerContextConstructorInvokers = new();
    private readonly Dictionary<Type, MessageHandlerMethodInvoker> _messageHandlerMethodInvokers = new();
    private readonly Dictionary<Type, DirectMessageHandlerMethodInvoker> _directMessageHandlerMethodInvokers = new();
    private readonly SemaphoreSlim _lock = new(1, 1);

    public async ValueTask<bool> InvokeAsync(IPipelineContext<HandleMessage> pipelineContext, CancellationToken cancellationToken = default)
    {
        var state = Guard.AgainstNull(pipelineContext).Pipeline.State;
        var message = Guard.AgainstNull(state.GetMessage());
        var messageType = message.GetType();
        var transportMessage = Guard.AgainstNull(state.GetTransportMessage());

        var contextHandler = _serviceProvider.GetService(MessageHandlerType.MakeGenericType(messageType));

        if (contextHandler != null)
        {
            MessageHandlerMethodInvoker? contextHandlerMethodInvoker;

            await _lock.WaitAsync(cancellationToken).ConfigureAwait(false);

            try
            {
                if (!_messageHandlerMethodInvokers.TryGetValue(messageType, out contextHandlerMethodInvoker))
                {
                    var interfaceType = MessageHandlerType.MakeGenericType(messageType);
                    var method = contextHandler.GetType().GetInterfaceMap(interfaceType).TargetMethods.SingleOrDefault();

                    if (method == null)
                    {
                        throw new MessageHandlerInvokerException(string.Format(Resources.HandlerMessageMethodMissingException, contextHandler.GetType().FullName, messageType.FullName));
                    }

                    var methodInfo = contextHandler.GetType().GetInterfaceMap(MessageHandlerType.MakeGenericType(messageType)).TargetMethods.SingleOrDefault();

                    if (methodInfo == null)
                    {
                        throw new MessageHandlerInvokerException(string.Format(Resources.HandlerMessageMethodMissingException, contextHandler.GetType().FullName, messageType.FullName));
                    }

                    contextHandlerMethodInvoker = new(methodInfo);

                    _messageHandlerMethodInvokers.Add(messageType, contextHandlerMethodInvoker);
                }
            }
            finally
            {
                _lock.Release();
            }

            var handlerContext = await GetHandlerContextAsync(state, messageType, transportMessage, message, cancellationToken);

            await contextHandlerMethodInvoker.InvokeAsync(contextHandler, handlerContext, cancellationToken).ConfigureAwait(false);

            return true;
        }

        var messageHandler = _serviceProvider.GetService(DirectMessageHandlerType.MakeGenericType(messageType));

        if (messageHandler != null)
        {
            DirectMessageHandlerMethodInvoker? messageHandlerMethodInvoker;

            await _lock.WaitAsync(cancellationToken).ConfigureAwait(false);

            try
            {
                if (!_directMessageHandlerMethodInvokers.TryGetValue(messageType, out messageHandlerMethodInvoker))
                {
                    var interfaceType = DirectMessageHandlerType.MakeGenericType(messageType);
                    var method = messageHandler.GetType().GetInterfaceMap(interfaceType).TargetMethods.SingleOrDefault();

                    if (method == null)
                    {
                        throw new MessageHandlerInvokerException(string.Format(Resources.HandlerMessageMethodMissingException, messageHandler.GetType().FullName, messageType.FullName));
                    }

                    var methodInfo = messageHandler.GetType().GetInterfaceMap(DirectMessageHandlerType.MakeGenericType(messageType)).TargetMethods.SingleOrDefault();

                    if (methodInfo == null)
                    {
                        throw new MessageHandlerInvokerException(string.Format(Resources.HandlerMessageMethodMissingException, messageHandler.GetType().FullName, messageType.FullName));
                    }

                    messageHandlerMethodInvoker = new(methodInfo);

                    _directMessageHandlerMethodInvokers.Add(messageType, messageHandlerMethodInvoker);
                }
            }
            finally
            {
                _lock.Release();
            }

            await messageHandlerMethodInvoker.InvokeAsync(messageHandler, message, cancellationToken).ConfigureAwait(false);

            return true;
        }

        if (_messageHandlerDelegateRegistry.TryGetValue(messageType, out var contextHandlerDelegate))
        {
            var handlerContext = await GetHandlerContextAsync(state, messageType, transportMessage, message, cancellationToken);

            await (Task)contextHandlerDelegate!.Handler.DynamicInvoke(contextHandlerDelegate.GetParameters(_serviceProvider, handlerContext, cancellationToken))!;

            return true;
        }

        if (_directMessageHandlerDelegateRegistry.TryGetValue(messageType, out var messageHandlerDelegate))
        {
            await (Task)messageHandlerDelegate!.Handler.DynamicInvoke(messageHandlerDelegate.GetParameters(_serviceProvider, message, cancellationToken))!;

            return true;
        }

        return false;
    }

    private async ValueTask<object> GetHandlerContextAsync(IState state, Type messageType, TransportMessage transportMessage, object message, CancellationToken cancellationToken)
    {
        HandlerContextConstructorInvoker? handlerContextConstructor;

        await _lock.WaitAsync(cancellationToken).ConfigureAwait(false);

        try
        {
            if (!_handlerContextConstructorInvokers.TryGetValue(messageType, out handlerContextConstructor))
            {
                handlerContextConstructor = new(messageType);

                _handlerContextConstructorInvokers.TryAdd(messageType, handlerContextConstructor);
            }
        }
        finally
        {
            _lock.Release();
        }

        var handlerContext = handlerContextConstructor.CreateHandlerContext(Guard.AgainstNull(_messageSender), Guard.AgainstNull(transportMessage), message);

        state.SetHandlerContext(handlerContext);

        return handlerContext;
    }
}