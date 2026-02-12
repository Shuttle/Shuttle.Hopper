using Microsoft.Extensions.DependencyInjection;
using Shuttle.Core.Contract;
using Shuttle.Core.Pipelines;

namespace Shuttle.Hopper;

public class MessageHandlerInvoker(IContextMessageHandlerDelegateRegistry contextMessageHandlerDelegateRegistry, IMessageHandlerDelegateRegistry messageHandlerDelegateRegistry)
    : IMessageHandlerInvoker
{
    private readonly IContextMessageHandlerDelegateRegistry _contextMessageHandlerDelegateRegistry = Guard.AgainstNull(contextMessageHandlerDelegateRegistry);
    private readonly IMessageHandlerDelegateRegistry _messageHandlerDelegateRegistry = Guard.AgainstNull(messageHandlerDelegateRegistry);
    private static readonly Type ContextMessageHandlerType = typeof(IContextMessageHandler<>);
    private static readonly Type DirectMessageHandlerType = typeof(IMessageHandler<>);
    private readonly Dictionary<Type, HandlerContextConstructorInvoker> _handlerContextConstructorInvokers = new();
    private readonly Dictionary<Type, ContextMessageHandlerMethodInvoker> _messageHandlerMethodInvokers = new();
    private readonly Dictionary<Type, MessageHandlerMethodInvoker> _directMessageHandlerMethodInvokers = new();
    private readonly SemaphoreSlim _lock = new(1, 1);

    public async ValueTask<bool> InvokeAsync(IPipelineContext<HandleMessage> pipelineContext, CancellationToken cancellationToken = default)
    {
        var state = Guard.AgainstNull(pipelineContext).Pipeline.State;
        var message = Guard.AgainstNull(state.GetMessage());
        var messageType = message.GetType();
        var transportMessage = Guard.AgainstNull(state.GetTransportMessage());
        var serviceProvider = pipelineContext.Pipeline.ServiceProvider;

        var messageContext = serviceProvider.GetRequiredService<IMessageContext>();
        var messageSenderContext = serviceProvider.GetRequiredService<IMessageSenderContext>();
        var messageSender = serviceProvider.GetRequiredService<IMessageSender>();

        messageContext.TransportMessage = transportMessage;
        messageSenderContext.TransportMessage = transportMessage;
        
        var contextHandler = serviceProvider.GetService(ContextMessageHandlerType.MakeGenericType(messageType));

        if (contextHandler != null)
        {
            ContextMessageHandlerMethodInvoker? contextHandlerMethodInvoker;

            await _lock.WaitAsync(cancellationToken).ConfigureAwait(false);

            try
            {
                if (!_messageHandlerMethodInvokers.TryGetValue(messageType, out contextHandlerMethodInvoker))
                {
                    var interfaceType = ContextMessageHandlerType.MakeGenericType(messageType);
                    var method = contextHandler.GetType().GetInterfaceMap(interfaceType).TargetMethods.SingleOrDefault();

                    if (method == null)
                    {
                        throw new MessageHandlerInvokerException(string.Format(Resources.HandlerMessageMethodMissingException, contextHandler.GetType().FullName, messageType.FullName));
                    }

                    var methodInfo = contextHandler.GetType().GetInterfaceMap(ContextMessageHandlerType.MakeGenericType(messageType)).TargetMethods.SingleOrDefault();

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

            var handlerContext = await GetHandlerContextAsync(state, messageSender, messageContext, messageType, transportMessage, message, cancellationToken);

            await contextHandlerMethodInvoker.InvokeAsync(contextHandler, handlerContext, cancellationToken).ConfigureAwait(false);

            return true;
        }

        var messageHandler = serviceProvider.GetService(DirectMessageHandlerType.MakeGenericType(messageType));

        if (messageHandler != null)
        {
            MessageHandlerMethodInvoker? messageHandlerMethodInvoker;

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

        if (_contextMessageHandlerDelegateRegistry.TryGetValue(messageType, out var contextHandlerDelegate))
        {
            var handlerContext = await GetHandlerContextAsync(state, messageSender, messageContext, messageType, transportMessage, message, cancellationToken);

            await (Task)contextHandlerDelegate!.Handler.DynamicInvoke(contextHandlerDelegate.GetParameters(serviceProvider, handlerContext, cancellationToken))!;

            return true;
        }

        if (_messageHandlerDelegateRegistry.TryGetValue(messageType, out var messageHandlerDelegate))
        {
            await (Task)messageHandlerDelegate!.Handler.DynamicInvoke(messageHandlerDelegate.GetParameters(serviceProvider, message, cancellationToken))!;

            return true;
        }

        return false;
    }

    private async ValueTask<object> GetHandlerContextAsync(IState state, IMessageSender messageSender, IMessageContext messageContext, Type messageType, TransportMessage transportMessage, object message, CancellationToken cancellationToken)
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

        var handlerContext = handlerContextConstructor.CreateHandlerContext(messageSender, messageContext, transportMessage, message);

        state.SetHandlerContext(handlerContext);

        return handlerContext;
    }
}
