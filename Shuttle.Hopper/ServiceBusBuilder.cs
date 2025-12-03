using System.Collections.ObjectModel;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Shuttle.Core.Contract;
using Shuttle.Core.Pipelines;
using Shuttle.Core.Reflection;

namespace Shuttle.Hopper;

public class ServiceBusBuilder(IServiceCollection services)
{
    private static readonly Type MessageHandlerType = typeof(IMessageHandler<>);
    private readonly Dictionary<Type, MessageHandlerDelegate> _delegates = new();

    private ServiceBusOptions _serviceBusOptions = new();

    public ServiceBusOptions Options
    {
        get => _serviceBusOptions;
        set => _serviceBusOptions = value ?? throw new ArgumentNullException(nameof(value));
    }

    public IServiceCollection Services { get; } = Guard.AgainstNull(services);

    public bool ShouldSuppressHostedService { get; private set; }
    public bool ShouldSuppressPipelineProcessing { get; private set; }

    public ServiceBusBuilder AddMessageHandler(Delegate handler)
    {
        if (!typeof(Task).IsAssignableFrom(Guard.AgainstNull(handler).Method.ReturnType))
        {
            throw new ApplicationException(Core.Pipelines.Resources.AsyncDelegateRequiredException);
        }

        var parameters = handler.Method.GetParameters();

        Type? messageType = null;

        foreach (var parameter in parameters)
        {
            var parameterType = parameter.ParameterType;

            if (parameterType.IsCastableTo(typeof(IHandlerContext<>)))
            {
                messageType = parameterType.GetGenericArguments()[0];
            }
        }

        if (messageType == null)
        {
            throw new ApplicationException(Resources.MessageHandlerTypeException);
        }

        if (!_delegates.TryAdd(messageType, new(handler, handler.Method.GetParameters().Select(item => item.ParameterType))))
        {
            throw new InvalidOperationException(string.Format(Resources.DelegateAlreadyRegisteredException, messageType.FullName));
        }

        return this;
    }

    public ServiceBusBuilder AddMessageHandler(object messageHandler)
    {
        var type = Guard.AgainstNull(messageHandler).GetType();

        foreach (var @interface in type.InterfacesCastableTo(MessageHandlerType))
        {
            var genericType = MessageHandlerType.MakeGenericType(@interface.GetGenericArguments()[0]);
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
        getServiceLifetime ??= _ => ServiceLifetime.Singleton;

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

        var messageTypes = _serviceBusOptions.Subscription.MessageTypes;

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

    public IDictionary<Type, MessageHandlerDelegate> GetDelegates()
    {
        return new ReadOnlyDictionary<Type, MessageHandlerDelegate>(_delegates);
    }

    public void OnAddPipelineProcessing(PipelineProcessingBuilder pipelineProcessingBuilder)
    {
        //TODO: AddPipelineProcessing?.Invoke(this, new(pipelineProcessingBuilder));
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