using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Shuttle.Core.Compression;
using Shuttle.Core.Contract;
using Shuttle.Core.Encryption;
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
            services.TryAddSingleton<IEncryptionService, EncryptionService>();
            services.TryAddSingleton<ICompressionService, CompressionService>();
            services.TryAddSingleton<IBusConfiguration, BusConfiguration>();
            services.TryAddSingleton<IMemoryCache, MemoryCache>();

            services.TryAddSingleton<IDeferredMessageProcessorContext, DeferredMessageProcessorContext>();
            services.TryAddKeyedScoped<IProcessor, DeferredMessageProcessor>("DeferredMessageProcessor");
            services.TryAddKeyedScoped<IProcessor, InboxProcessor>("InboxProcessor");
            services.TryAddKeyedScoped<IProcessor, OutboxProcessor>("OutboxProcessor");

            services.AddThreading(threadingBuilder =>
            {
                threadingBuilder.ConfigureProcessorIdle("InboxProcessor", options =>
                {
                    options.Durations = hopperBuilder.Options.Inbox.IdleDurations.Any()
                        ? hopperBuilder.Options.Inbox.IdleDurations
                        : HopperOptions.DefaultIdleDurations.ToList();
                });

                threadingBuilder.ConfigureProcessorIdle("OutboxProcessor", options =>
                {
                    options.Durations = hopperBuilder.Options.Outbox.IdleDurations.Any()
                        ? hopperBuilder.Options.Outbox.IdleDurations
                        : HopperOptions.DefaultIdleDurations.ToList();
                });

                threadingBuilder.ConfigureProcessorIdle("DeferredMessageProcessor", options =>
                {
                    options.Durations = [hopperBuilder.Options.Inbox.DeferredMessageProcessorIdleDuration];
                });
            });

            services.AddPipelines(pipelineBuilder =>
            {
                pipelineBuilder.AddAssembly(typeof(Bus).Assembly);
            });

            services.AddTransactionScope();

            services.AddOptions<HopperOptions>().Configure(options =>
            {
                options.SuppressBusHostedService |= hopperBuilder.Options.SuppressBusHostedService;

                options.Inbox = hopperBuilder.Options.Inbox;
                options.Outbox = hopperBuilder.Options.Outbox;

                IServiceCollection.ApplyDefaults(options.Inbox);
                IServiceCollection.ApplyDefaults(options.Outbox);

                options.AddMessageHandlers = hopperBuilder.Options.AddMessageHandlers;
                options.CacheIdentity = hopperBuilder.Options.CacheIdentity;
                options.CompressionAlgorithm = hopperBuilder.Options.CompressionAlgorithm;
                options.CreatePhysicalTransports = hopperBuilder.Options.CreatePhysicalTransports;
                options.EncryptionAlgorithm = hopperBuilder.Options.EncryptionAlgorithm;
                options.RemoveCorruptMessages = hopperBuilder.Options.RemoveCorruptMessages;

                options.UriMappings = hopperBuilder.Options.UriMappings;
                options.MessageRoutes = hopperBuilder.Options.MessageRoutes;
                options.Subscription = hopperBuilder.Options.Subscription;
            });

            services.TryAddSingleton<IMessageHandlerDelegateRegistry>(_ => new MessageHandlerDelegateRegistry(hopperBuilder.GetMessageHandlerDelegates()));
            services.TryAddSingleton<IDirectMessageHandlerDelegateRegistry>(_ => new DirectMessageHandlerDelegateRegistry(hopperBuilder.GetDirectMessageHandlerDelegates()));

            if (hopperBuilder.Options.AddMessageHandlers)
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

        private static void ApplyDefaults(ProcessorOptions? processorOptions)
        {
            if (processorOptions == null)
            {
                return;
            }

            if (!processorOptions.IgnoreOnFailureDurations.Any())
            {
                processorOptions.IgnoreOnFailureDurations = [..HopperOptions.DefaultIgnoreOnFailureDurations];
            }

            if (!processorOptions.IdleDurations.Any())
            {
                processorOptions.IdleDurations = [..HopperOptions.DefaultIdleDurations];
            }
        }
    }
}