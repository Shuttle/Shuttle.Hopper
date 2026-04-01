using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Shuttle.Core.Contract;
using Shuttle.Core.Pipelines;
using Shuttle.Core.Serialization;
using Shuttle.Core.System;
using Shuttle.Core.Threading;
using Shuttle.Core.TransactionScope;

namespace Shuttle.Hopper;

public static class ServiceCollectionExtensions
{
    extension(IServiceCollection services)
    {
        public HopperBuilder AddHopper(Action<HopperOptions>? configureOptions = null)
        {
            var builder = new HopperBuilder(Guard.AgainstNull(services));

            services.AddOptions();
            services.AddOptions<HopperOptions>().Configure(options =>
            {
                configureOptions?.Invoke(options);

                options.Subscription.MessageTypes.AddRange(builder.SubscriptionMessageTypes);
            });

            services.TryAddSingleton<IEnvironmentService, EnvironmentService>();
            services.TryAddSingleton<IProcessService, ProcessService>();
            services.TryAddSingleton<ISerializer, JsonSerializer>();
            services.TryAddSingleton<IBusPolicy, DefaultBusPolicy>();
            services.TryAddSingleton<IMessageRouteProvider, MessageRouteProvider>();
            services.TryAddSingleton<IIdentityProvider, DefaultIdentityProvider>();
            services.TryAddSingleton<IUriResolver, UriResolver>();
            services.TryAddSingleton<ITransportService, TransportService>();
            services.TryAddSingleton<ITransportFactoryService, TransportFactoryService>();
            services.TryAddSingleton<ISubscriptionService, SubscriptionService>();
            services.TryAddSingleton<ISubscriptionQuery, NullSubscriptionQuery>();
            services.TryAddSingleton<IBusConfiguration, BusConfiguration>();
            services.TryAddSingleton<IMemoryCache, MemoryCache>();

            services.TryAddSingleton<IDeferredMessageProcessorContext, DeferredMessageProcessorContext>();
            services.TryAddKeyedScoped<IProcessor, DeferredMessageProcessor>("DeferredMessageProcessor");
            services.TryAddKeyedScoped<IProcessor, InboxProcessor>("InboxProcessor");
            services.TryAddKeyedScoped<IProcessor, OutboxProcessor>("OutboxProcessor");

            services.AddTransactionScope();
            services.AddPipelines().AddAssembly(typeof(Bus).Assembly);
            services.AddThreading()
                    .ConfigureProcessor("InboxProcessor", (options, serviceProvider) =>
                    {
                        var hopperOptions = serviceProvider.GetRequiredService<IOptions<HopperOptions>>().Value;
                        options.Durations = hopperOptions.Inbox.IdleDurations.Count > 0
                            ? hopperOptions.Inbox.IdleDurations
                            : HopperOptions.DefaultIdleDurations.ToList();
                    })
                    .ConfigureProcessor("OutboxProcessor", (options, serviceProvider) =>
                    {
                        var hopperOptions = serviceProvider.GetRequiredService<IOptions<HopperOptions>>().Value;
                        options.Durations = hopperOptions.Outbox.IdleDurations.Count > 0
                            ? hopperOptions.Outbox.IdleDurations
                            : HopperOptions.DefaultIdleDurations.ToList();
                    })
                .ConfigureProcessor("DeferredMessageProcessor", (options, serviceProvider) =>
                    {
                        var hopperOptions = serviceProvider.GetRequiredService<IOptions<HopperOptions>>().Value;
                        options.Durations = [hopperOptions.Inbox.DeferredMessageProcessorIdleDuration];
                    });

            services.TryAddSingleton<IContextMessageHandlerDelegateRegistry>(_ => new ContextMessageHandlerDelegateRegistry(builder.GetMessageHandlerDelegates()));
            services.TryAddSingleton<IMessageHandlerDelegateRegistry>(_ => new MessageHandlerDelegateRegistry(builder.GetDirectMessageHandlerDelegates()));

            services.TryAddSingleton<IMessageHandlerInvoker, MessageHandlerInvoker>();
            services.TryAddScoped<IMessageSender, MessageSender>();
            services.TryAddScoped<IMessageContext, MessageContext>();
            services.TryAddScoped<IMessageSenderContext, MessageSenderContext>();
            services.TryAddScoped<IBus, Bus>();
            services.TryAddSingleton<IBusControl, BusControl>();
            services.TryAddEnumerable(ServiceDescriptor.Singleton<IHostedService, BusHostedService>());

            return builder;
        }
    }
}