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
        public IServiceCollection AddHopper(Action<HopperBuilder>? builder = null)
        {
            var hopperBuilder = new HopperBuilder(Guard.AgainstNull(services));

            builder?.Invoke(hopperBuilder);

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

            hopperBuilder.ApplyOptions();

            services
                .AddTransactionScope()
                .AddThreading(threadingBuilder =>
                {
                    threadingBuilder.Configure("InboxProcessor", (options, sp) =>
                    {
                        var hopperOptions = sp.GetRequiredService<IOptions<HopperOptions>>().Value;
                        options.Durations = hopperOptions.Inbox.IdleDurations.Any()
                            ? hopperOptions.Inbox.IdleDurations
                            : HopperOptions.DefaultIdleDurations.ToList();
                    });

                    threadingBuilder.Configure("OutboxProcessor", (options, sp) =>
                    {
                        var hopperOptions = sp.GetRequiredService<IOptions<HopperOptions>>().Value;
                        options.Durations = hopperOptions.Outbox.IdleDurations.Any()
                            ? hopperOptions.Outbox.IdleDurations
                            : HopperOptions.DefaultIdleDurations.ToList();
                    });

                    threadingBuilder.Configure("DeferredMessageProcessor", (options, sp) =>
                    {
                        var hopperOptions = sp.GetRequiredService<IOptions<HopperOptions>>().Value;
                        options.Durations = [hopperOptions.Inbox.DeferredMessageProcessorIdleDuration];
                    });
                })
                .AddPipelines(pipelineBuilder =>
                {
                    pipelineBuilder.AddAssembly(typeof(Bus).Assembly);
                });

            services.TryAddSingleton<IContextMessageHandlerDelegateRegistry>(_ => new ContextMessageHandlerDelegateRegistry(hopperBuilder.GetMessageHandlerDelegates()));
            services.TryAddSingleton<IMessageHandlerDelegateRegistry>(_ => new MessageHandlerDelegateRegistry(hopperBuilder.GetDirectMessageHandlerDelegates()));

            if (hopperBuilder.ShouldRegisterMessageHandler)
            {
                foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
                {
                    hopperBuilder.AddMessageHandlers(assembly);
                }
            }
            else
            {
                hopperBuilder.AddMessageHandlers(typeof(Bus).Assembly);
            }

            services.TryAddSingleton<IMessageHandlerInvoker, MessageHandlerInvoker>();
            services.TryAddScoped<IMessageSender, MessageSender>();
            services.TryAddScoped<IMessageContext, MessageContext>();
            services.TryAddScoped<IMessageSenderContext, MessageSenderContext>();
            services.TryAddScoped<IBus, Bus>();
            services.TryAddSingleton<IBusControl, BusControl>();

            services.TryAddEnumerable(ServiceDescriptor.Singleton<IHostedService, BusHostedService>());

            return services;
        }
    }
}