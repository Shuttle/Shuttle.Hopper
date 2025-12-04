using Shuttle.Core.Contract;
using Shuttle.Core.Pipelines;

namespace Shuttle.Hopper;

public class MessageHandlerInvoker(IServiceProvider serviceProvider, IMessageSender messageSender, IContextHandlerRegistry contextHandlerRegistry)
    : IMessageHandlerInvoker
{
    private readonly IContextHandlerRegistry _contextHandlerRegistry = Guard.AgainstNull(contextHandlerRegistry);
    private static readonly Type ContextHandlerType = typeof(IContextHandler<>);
    private readonly Dictionary<Type, HandlerContextConstructorInvoker> _handlerContextConstructorInvokers = new();
    private readonly Dictionary<Type, ProcessMessageMethodInvoker> _processMessageMethodInvokers = new();
    private readonly SemaphoreSlim _lock = new(1, 1);
    private readonly IMessageSender _messageSender = Guard.AgainstNull(messageSender);
    private readonly IServiceProvider _serviceProvider = Guard.AgainstNull(serviceProvider);

    public async ValueTask<bool> InvokeAsync(IPipelineContext<OnHandleMessage> pipelineContext, CancellationToken cancellationToken = default)
    {
        var state = Guard.AgainstNull(pipelineContext).Pipeline.State;
        var message = Guard.AgainstNull(state.GetMessage());
        var messageType = message.GetType();
        var transportMessage = Guard.AgainstNull(state.GetTransportMessage());

        HandlerContextConstructorInvoker? handlerContextConstructor;

        await _lock.WaitAsync(pipelineContext.Pipeline.CancellationToken).ConfigureAwait(false);

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

        if (_contextHandlerRegistry.TryGetValue(messageType, out var messageHandlerDelegate))
        {
            await (Task)messageHandlerDelegate!.Handler.DynamicInvoke(messageHandlerDelegate.GetParameters(_serviceProvider, handlerContext, cancellationToken))!;

            return true;
        }

        var handler = _serviceProvider.GetService(ContextHandlerType.MakeGenericType(messageType));

        if (handler == null)
        {
            return false;
        }

        ProcessMessageMethodInvoker? processMessageMethodInvoker;

        await _lock.WaitAsync(pipelineContext.Pipeline.CancellationToken).ConfigureAwait(false);

        try
        {
            if (!_processMessageMethodInvokers.TryGetValue(messageType, out processMessageMethodInvoker))
            {
                var interfaceType = ContextHandlerType.MakeGenericType(messageType);
                var method = handler.GetType().GetInterfaceMap(interfaceType).TargetMethods.SingleOrDefault();

                if (method == null)
                {
                    throw new MessageHandlerInvokerException(string.Format(Resources.HandlerMessageMethodMissingException, handler.GetType().FullName, messageType.FullName));
                }

                var methodInfo = handler.GetType().GetInterfaceMap(ContextHandlerType.MakeGenericType(messageType)).TargetMethods.SingleOrDefault();

                if (methodInfo == null)
                {
                    throw new MessageHandlerInvokerException(string.Format(Resources.HandlerMessageMethodMissingException, handler.GetType().FullName, messageType.FullName));
                }

                processMessageMethodInvoker = new(methodInfo);

                _processMessageMethodInvokers.Add(messageType, processMessageMethodInvoker);
            }
        }
        finally
        {
            _lock.Release();
        }

        await processMessageMethodInvoker.InvokeAsync(handler, handlerContext, cancellationToken).ConfigureAwait(false);

        return true;
    }
}