using Shuttle.Core.Contract;
using Shuttle.Core.Pipelines;

namespace Shuttle.Hopper;

public class MessageHandlerInvoker(IServiceProvider serviceProvider, IMessageSender messageSender, IContextHandlerDelegateRegistry contextHandlerDelegateRegistry, IMessageHandlerDelegateRegistry messageHandlerDelegateRegistry)
    : IMessageHandlerInvoker
{
    private readonly IServiceProvider _serviceProvider = Guard.AgainstNull(serviceProvider);
    private readonly IMessageSender _messageSender = Guard.AgainstNull(messageSender);
    private readonly IContextHandlerDelegateRegistry _contextHandlerDelegateRegistry = Guard.AgainstNull(contextHandlerDelegateRegistry);
    private readonly IMessageHandlerDelegateRegistry _messageHandlerDelegateRegistry = Guard.AgainstNull(messageHandlerDelegateRegistry);
    private static readonly Type ContextHandlerType = typeof(IContextHandler<>);
    private static readonly Type MessageHandlerType = typeof(IMessageHandler<>);
    private readonly Dictionary<Type, HandlerContextConstructorInvoker> _handlerContextConstructorInvokers = new();
    private readonly Dictionary<Type, ContextHandlerMethodInvoker> _contextHandlerMethodInvokers = new();
    private readonly Dictionary<Type, MessageHandlerMethodInvoker> _messageHandlerMethodInvokers = new();
    private readonly SemaphoreSlim _lock = new(1, 1);

    public async ValueTask<bool> InvokeAsync(IPipelineContext<HandleMessage> pipelineContext, CancellationToken cancellationToken = default)
    {
        var state = Guard.AgainstNull(pipelineContext).Pipeline.State;
        var message = Guard.AgainstNull(state.GetMessage());
        var messageType = message.GetType();
        var transportMessage = Guard.AgainstNull(state.GetTransportMessage());

        var contextHandler = _serviceProvider.GetService(ContextHandlerType.MakeGenericType(messageType));

        if (contextHandler != null)
        {
            ContextHandlerMethodInvoker? contextHandlerMethodInvoker;

            await _lock.WaitAsync(cancellationToken).ConfigureAwait(false);

            try
            {
                if (!_contextHandlerMethodInvokers.TryGetValue(messageType, out contextHandlerMethodInvoker))
                {
                    var interfaceType = ContextHandlerType.MakeGenericType(messageType);
                    var method = contextHandler.GetType().GetInterfaceMap(interfaceType).TargetMethods.SingleOrDefault();

                    if (method == null)
                    {
                        throw new MessageHandlerInvokerException(string.Format(Resources.HandlerMessageMethodMissingException, contextHandler.GetType().FullName, messageType.FullName));
                    }

                    var methodInfo = contextHandler.GetType().GetInterfaceMap(ContextHandlerType.MakeGenericType(messageType)).TargetMethods.SingleOrDefault();

                    if (methodInfo == null)
                    {
                        throw new MessageHandlerInvokerException(string.Format(Resources.HandlerMessageMethodMissingException, contextHandler.GetType().FullName, messageType.FullName));
                    }

                    contextHandlerMethodInvoker = new(methodInfo);

                    _contextHandlerMethodInvokers.Add(messageType, contextHandlerMethodInvoker);
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

        var messageHandler = _serviceProvider.GetService(MessageHandlerType.MakeGenericType(messageType));

        if (messageHandler != null)
        {
            MessageHandlerMethodInvoker? messageHandlerMethodInvoker;

            await _lock.WaitAsync(cancellationToken).ConfigureAwait(false);

            try
            {
                if (!_messageHandlerMethodInvokers.TryGetValue(messageType, out messageHandlerMethodInvoker))
                {
                    var interfaceType = MessageHandlerType.MakeGenericType(messageType);
                    var method = messageHandler.GetType().GetInterfaceMap(interfaceType).TargetMethods.SingleOrDefault();

                    if (method == null)
                    {
                        throw new MessageHandlerInvokerException(string.Format(Resources.HandlerMessageMethodMissingException, messageHandler.GetType().FullName, messageType.FullName));
                    }

                    var methodInfo = messageHandler.GetType().GetInterfaceMap(MessageHandlerType.MakeGenericType(messageType)).TargetMethods.SingleOrDefault();

                    if (methodInfo == null)
                    {
                        throw new MessageHandlerInvokerException(string.Format(Resources.HandlerMessageMethodMissingException, messageHandler.GetType().FullName, messageType.FullName));
                    }

                    messageHandlerMethodInvoker = new(methodInfo);

                    _messageHandlerMethodInvokers.Add(messageType, messageHandlerMethodInvoker);
                }
            }
            finally
            {
                _lock.Release();
            }

            await messageHandlerMethodInvoker.InvokeAsync(messageHandler, message, cancellationToken).ConfigureAwait(false);

            return true;
        }

        if (_contextHandlerDelegateRegistry.TryGetValue(messageType, out var contextHandlerDelegate))
        {
            var handlerContext = await GetHandlerContextAsync(state, messageType, transportMessage, message, cancellationToken);

            await (Task)contextHandlerDelegate!.Handler.DynamicInvoke(contextHandlerDelegate.GetParameters(_serviceProvider, handlerContext, cancellationToken))!;

            return true;
        }

        if (_messageHandlerDelegateRegistry.TryGetValue(messageType, out var messageHandlerDelegate))
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