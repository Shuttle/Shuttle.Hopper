using Microsoft.Extensions.DependencyInjection;
using Shuttle.Core.Contract;
using Shuttle.Core.Reflection;
using System.Collections.ObjectModel;
using System.Reflection;

namespace Shuttle.Hopper;

public class ServiceBusBuilder(IServiceCollection services)
{
    private static readonly Type ContextHandlerType = typeof(IContextHandler<>);
    private static readonly Type MessageHandlerType = typeof(IMessageHandler<>);
    private readonly Dictionary<Type, ContextHandlerDelegate> _contextHandlerDelegates = new();
    private readonly Dictionary<Type, MessageHandlerDelegate> _messageHandlerDelegates = new();

    public ServiceBusOptions Options
    {
        get;
        set => field = value ?? throw new ArgumentNullException(nameof(value));
    } = new();

    public IServiceCollection Services { get; } = Guard.AgainstNull(services);

    public bool ShouldSuppressHostedService { get; private set; }
    public bool ShouldSuppressPipelineProcessing { get; private set; }

    public ServiceBusBuilder AddMessageHandler<TDelegate>(TDelegate handler) where TDelegate : Delegate
    {
        var returnType = handler.Method.ReturnType;

        if (!typeof(Task).IsAssignableFrom(returnType) && !typeof(ValueTask).IsAssignableFrom(returnType))
        {
            throw new ApplicationException(Core.Pipelines.Resources.AsyncDelegateRequiredException);
        }

        var parameters = handler.Method.GetParameters();

        if (parameters.Length < 1)
        {
            throw new ApplicationException(Resources.MessageHandlerTypeException);
        }

        var parameterType = parameters[0].ParameterType;

        Type messageType;

        if (parameterType.IsCastableTo(typeof(IHandlerContext<>)))
        {
            messageType = parameterType.GetGenericArguments()[0];

            if (!_contextHandlerDelegates.TryAdd(messageType, new(handler, parameters.Select(item => item.ParameterType))))
            {
                throw new InvalidOperationException(string.Format(Resources.DelegateAlreadyRegisteredException, messageType.FullName));
            }
        }
        else
        {
            messageType = parameterType;

            if (messageType.IsInterface)
            {
                throw new ApplicationException(Resources.MessageHandlerTypeException);
            }

            if (!_messageHandlerDelegates.TryAdd(messageType, new(handler, parameters.Select(item => item.ParameterType))))
            {
                throw new InvalidOperationException(string.Format(Resources.DelegateAlreadyRegisteredException, messageType.FullName));
            }
        }

        return this;
    }

    public ServiceBusBuilder AddMessageHandler(object messageHandler)
    {
        var type = Guard.AgainstNull(messageHandler).GetType();

        foreach (var @interface in type.InterfacesCastableTo(ContextHandlerType))
        {
            var genericType = ContextHandlerType.MakeGenericType(@interface.GetGenericArguments()[0]);
            var serviceDescriptor = new ServiceDescriptor(genericType, type, ServiceLifetime.Singleton);

            if (Services.Contains(serviceDescriptor))
            {
                throw new InvalidOperationException(string.Format(Resources.MessageHandlerAlreadyRegisteredException, type.FullName));
            }

            Services.Add(serviceDescriptor);
        }

        foreach (var @interface in type.InterfacesCastableTo(MessageHandlerType))
        {
            var genericType = ContextHandlerType.MakeGenericType(@interface.GetGenericArguments()[0]);
            var serviceDescriptor = new ServiceDescriptor(genericType, type, ServiceLifetime.Singleton);

            if (Services.Contains(serviceDescriptor))
            {
                throw new InvalidOperationException(string.Format(Resources.MessageHandlerAlreadyRegisteredException, type.FullName));
            }

            Services.Add(serviceDescriptor);
        }

        return this;
    }

    public ServiceBusBuilder AddMessageHandlers(Assembly assembly, Func<Type, ServiceLifetime>? getServiceLifetime = null)
    {
        getServiceLifetime ??= _ => ServiceLifetime.Scoped;

        foreach (var type in Guard.AgainstNull(assembly).GetTypesCastableToAsync(ContextHandlerType).GetAwaiter().GetResult())
        foreach (var @interface in type.InterfacesCastableTo(ContextHandlerType))
        {
            var genericType = ContextHandlerType.MakeGenericType(@interface.GetGenericArguments()[0]);
            var serviceDescriptor = new ServiceDescriptor(genericType, type, getServiceLifetime(genericType));

            if (Services.Contains(serviceDescriptor))
            {
                throw new InvalidOperationException(string.Format(Resources.MessageHandlerAlreadyRegisteredException, type.FullName));
            }

            Services.Add(serviceDescriptor);
        }

        foreach (var type in Guard.AgainstNull(assembly).GetTypesCastableToAsync(MessageHandlerType).GetAwaiter().GetResult())
        foreach (var @interface in type.InterfacesCastableTo(MessageHandlerType))
        {
            var genericType = MessageHandlerType.MakeGenericType(@interface.GetGenericArguments()[0]);
            var serviceDescriptor = new ServiceDescriptor(genericType, type, getServiceLifetime(genericType));

            if (Services.Contains(serviceDescriptor))
            {
                throw new InvalidOperationException(string.Format(Resources.MessageHandlerAlreadyRegisteredException, type.FullName));
            }

            Services.Add(serviceDescriptor);
        }

        return this;
    }

    public ServiceBusBuilder AddSubscription<T>()
    {
        AddSubscription(typeof(T));

        return this;
    }

    public ServiceBusBuilder AddSubscription(Type messageType)
    {
        AddSubscription(Guard.AgainstEmpty(Guard.AgainstNull(messageType).FullName));

        return this;
    }

    public ServiceBusBuilder AddSubscription(string messageType)
    {
        Guard.AgainstEmpty(messageType);

        var messageTypes = Options.Subscription.MessageTypes;

        if (messageTypes == null)
        {
            throw new InvalidOperationException(Resources.AddSubscriptionException);
        }

        if (!messageTypes.Contains(messageType))
        {
            messageTypes.Add(messageType);
        }

        return this;
    }

    public IDictionary<Type, ContextHandlerDelegate> GetContextHandlerDelegates()
    {
        return new ReadOnlyDictionary<Type, ContextHandlerDelegate>(_contextHandlerDelegates);
    }

    public IDictionary<Type, MessageHandlerDelegate> GetMessageHandlerDelegates()
    {
        return new ReadOnlyDictionary<Type, MessageHandlerDelegate>(_messageHandlerDelegates);
    }

    public ServiceBusBuilder SuppressHostedService()
    {
        ShouldSuppressHostedService = true;

        return this;
    }

    public ServiceBusBuilder SuppressPipelineProcessing()
    {
        ShouldSuppressPipelineProcessing = true;

        return this;
    }
}