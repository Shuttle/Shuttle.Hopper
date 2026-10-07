using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Shuttle.Contract;
using Shuttle.Reflection;
using Shuttle.Threading;
using System.Collections.ObjectModel;
using System.Reflection;

namespace Shuttle.Hopper;

public class HopperBuilder(IServiceCollection services)
{
    private static readonly Type ContextMessageHandlerType = typeof(IContextMessageHandler<>);
    private static readonly Type DirectMessageHandlerType = typeof(IMessageHandler<>);
    private readonly Dictionary<Type, MessageHandlerDelegate> _messageHandlerDelegates = new();
    private readonly Dictionary<Type, DirectMessageHandlerDelegate> _directMessageHandlerDelegates = new();
    private readonly Dictionary<string, Action<InboxOptions>?> _additionalInboxes = new(StringComparer.OrdinalIgnoreCase);

    public IServiceCollection Services { get; } = Guard.AgainstNull(services);

    internal List<string> SubscriptionMessageTypes { get; } = [];

    internal IReadOnlyDictionary<string, Action<InboxOptions>?> AdditionalInboxes => _additionalInboxes;

    public HopperBuilder AddInbox(string name, Action<InboxOptions>? configureOptions = null)
    {
        Guard.AgainstEmpty(name);

        if (!_additionalInboxes.TryAdd(name, configureOptions))
        {
            throw new InvalidOperationException(string.Format(Resources.InboxAlreadyRegisteredException, name));
        }

        var serviceKey = InboxProcessor.GetServiceKey(name);
        var deferredServiceKey = DeferredMessageProcessor.GetServiceKey(name);

        Services.AddKeyedScoped<IProcessor>(serviceKey, (serviceProvider, _) =>
        {
            var inboxMessagePipeline = serviceProvider.GetRequiredService<IInboxMessagePipeline>();
            var busConfiguration = serviceProvider.GetRequiredService<IBusConfiguration>();
            var inboxOptions = serviceProvider.GetRequiredService<IOptions<HopperOptions>>().Value.AdditionalInboxes[name];

            // The pipeline constructor binds the primary inbox; the pipeline instance is scoped, so this is the same
            // instance that the processor executes.
            inboxMessagePipeline.State.BindInbox(busConfiguration.AdditionalInboxes[name], inboxOptions);

            if (inboxOptions.DeferredTransportUri != null)
            {
                inboxMessagePipeline.State.SetDeferredMessageProcessorContext(serviceProvider.GetRequiredKeyedService<IDeferredMessageProcessorContext>(deferredServiceKey));
            }

            return new InboxProcessor(inboxMessagePipeline);
        });

        Services.AddOptions<ProcessorIdleOptions>(serviceKey).Configure<IOptions<HopperOptions>>((options, hopperOptions) =>
        {
            var inboxOptions = hopperOptions.Value.AdditionalInboxes[name];

            options.Durations = inboxOptions.IdleDurations.Count > 0
                ? inboxOptions.IdleDurations
                : HopperOptions.DefaultIdleDurations.ToList();
        });

        // Only used when the additional inbox has its own deferred transport.
        Services.AddKeyedSingleton<IDeferredMessageProcessorContext>(deferredServiceKey, (serviceProvider, _) =>
        {
            var hopperOptions = serviceProvider.GetRequiredService<IOptions<HopperOptions>>().Value;

            return new DeferredMessageProcessorContext(hopperOptions, hopperOptions.AdditionalInboxes[name]);
        });

        Services.AddKeyedScoped<IProcessor>(deferredServiceKey, (serviceProvider, _) =>
        {
            var deferredMessagePipeline = serviceProvider.GetRequiredService<IDeferredMessagePipeline>();

            deferredMessagePipeline.State.BindDeferredInbox(serviceProvider.GetRequiredService<IBusConfiguration>().AdditionalInboxes[name], []);

            return new DeferredMessageProcessor(deferredMessagePipeline, serviceProvider.GetRequiredKeyedService<IDeferredMessageProcessorContext>(deferredServiceKey));
        });

        Services.AddOptions<ProcessorIdleOptions>(deferredServiceKey).Configure<IOptions<HopperOptions>>((options, hopperOptions) =>
        {
            options.Durations = [hopperOptions.Value.AdditionalInboxes[name].DeferredMessageProcessorIdleDuration];
        });

        return this;
    }

    public HopperBuilder AddMessageHandler<TDelegate>(TDelegate handler) where TDelegate : Delegate
    {
        var returnType = handler.Method.ReturnType;

        if (!typeof(Task).IsAssignableFrom(returnType) && !typeof(ValueTask).IsAssignableFrom(returnType))
        {
            throw new ApplicationException(Pipelines.Resources.AsyncDelegateRequiredException);
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

            if (!_messageHandlerDelegates.TryAdd(messageType, new(handler, parameters.Select(item => item.ParameterType))))
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

            if (!_directMessageHandlerDelegates.TryAdd(messageType, new(handler, parameters.Select(item => item.ParameterType))))
            {
                throw new InvalidOperationException(string.Format(Resources.DelegateAlreadyRegisteredException, messageType.FullName));
            }
        }

        return this;
    }

    public HopperBuilder AddMessageHandler(object messageHandler)
    {
        var type = Guard.AgainstNull(messageHandler).GetType();

        foreach (var @interface in type.InterfacesCastableTo(ContextMessageHandlerType))
        {
            var genericType = ContextMessageHandlerType.MakeGenericType(@interface.GetGenericArguments()[0]);
            var serviceDescriptor = new ServiceDescriptor(genericType, type, ServiceLifetime.Singleton);

            if (Services.Contains(serviceDescriptor))
            {
                throw new InvalidOperationException(string.Format(Resources.MessageHandlerAlreadyRegisteredException, type.FullName));
            }

            Services.Add(serviceDescriptor);
        }

        foreach (var @interface in type.InterfacesCastableTo(DirectMessageHandlerType))
        {
            var genericType = ContextMessageHandlerType.MakeGenericType(@interface.GetGenericArguments()[0]);
            var serviceDescriptor = new ServiceDescriptor(genericType, type, ServiceLifetime.Singleton);

            if (Services.Contains(serviceDescriptor))
            {
                throw new InvalidOperationException(string.Format(Resources.MessageHandlerAlreadyRegisteredException, type.FullName));
            }

            Services.Add(serviceDescriptor);
        }

        return this;
    }

    public HopperBuilder AddMessageHandler<T>(ServiceLifetime serviceLifetime = ServiceLifetime.Scoped)
    {
        return AddMessageHandler(typeof(T), serviceLifetime);
    }

    public HopperBuilder AddMessageHandler(Type type, ServiceLifetime serviceLifetime = ServiceLifetime.Scoped)
    {
        Guard.AgainstNull(type);

        foreach (var @interface in type.InterfacesCastableTo(ContextMessageHandlerType))
        {
            var genericType = ContextMessageHandlerType.MakeGenericType(@interface.GetGenericArguments()[0]);
            var serviceDescriptor = new ServiceDescriptor(genericType, type, serviceLifetime);

            if (Services.Contains(serviceDescriptor))
            {
                throw new InvalidOperationException(string.Format(Resources.MessageHandlerAlreadyRegisteredException, type.FullName));
            }

            Services.Add(serviceDescriptor);
        }

        foreach (var @interface in type.InterfacesCastableTo(DirectMessageHandlerType))
        {
            var genericType = DirectMessageHandlerType.MakeGenericType(@interface.GetGenericArguments()[0]);
            var serviceDescriptor = new ServiceDescriptor(genericType, type, serviceLifetime);

            if (Services.Contains(serviceDescriptor))
            {
                throw new InvalidOperationException(string.Format(Resources.MessageHandlerAlreadyRegisteredException, type.FullName));
            }

            Services.Add(serviceDescriptor);
        }

        return this;
    }

    public HopperBuilder AddMessageHandlersFrom(Assembly assembly, Func<Type, ServiceLifetime>? getServiceLifetime = null)
    {
        return AddMessageHandlersFrom([Guard.AgainstNull(assembly)], getServiceLifetime);
    }

    public HopperBuilder AddMessageHandlersFrom(Assembly[] assemblies, Func<Type, ServiceLifetime>? getServiceLifetime = null)
    {
        getServiceLifetime ??= _ => ServiceLifetime.Scoped;

        foreach (var type in assemblies.SelectMany(assembly => assembly.FindTypesCastableTo(ContextMessageHandlerType)))
        foreach (var @interface in type.InterfacesCastableTo(ContextMessageHandlerType))
        {
            var genericType = ContextMessageHandlerType.MakeGenericType(@interface.GetGenericArguments()[0]);
            var serviceDescriptor = new ServiceDescriptor(genericType, type, getServiceLifetime(genericType));

            if (Services.Contains(serviceDescriptor))
            {
                throw new InvalidOperationException(string.Format(Resources.MessageHandlerAlreadyRegisteredException, type.FullName));
            }

            Services.TryAdd(serviceDescriptor);
        }

        foreach (var type in assemblies.SelectMany(assembly => assembly.FindTypesCastableTo(DirectMessageHandlerType)))
        foreach (var @interface in type.InterfacesCastableTo(DirectMessageHandlerType))
        {
            var genericType = DirectMessageHandlerType.MakeGenericType(@interface.GetGenericArguments()[0]);
            var serviceDescriptor = new ServiceDescriptor(genericType, type, getServiceLifetime(genericType));

            if (Services.Contains(serviceDescriptor))
            {
                throw new InvalidOperationException(string.Format(Resources.MessageHandlerAlreadyRegisteredException, type.FullName));
            }

            Services.TryAdd(serviceDescriptor);
        }

        return this;
    }

    public HopperBuilder AddSubscription<T>()
    {
        AddSubscription(typeof(T));

        return this;
    }

    public HopperBuilder AddSubscription(Type messageType)
    {
        AddSubscription(Guard.AgainstEmpty(Guard.AgainstNull(messageType).FullName));

        return this;
    }

    public HopperBuilder AddSubscription(string messageType)
    {
        Guard.AgainstEmpty(messageType);

        if (!SubscriptionMessageTypes.Contains(messageType))
        {
            SubscriptionMessageTypes.Add(messageType);
        }

        return this;
    }

    public IDictionary<Type, MessageHandlerDelegate> GetMessageHandlerDelegates()
    {
        return new ReadOnlyDictionary<Type, MessageHandlerDelegate>(_messageHandlerDelegates);
    }

    public IDictionary<Type, DirectMessageHandlerDelegate> GetDirectMessageHandlerDelegates()
    {
        return new ReadOnlyDictionary<Type, DirectMessageHandlerDelegate>(_directMessageHandlerDelegates);
    }
}