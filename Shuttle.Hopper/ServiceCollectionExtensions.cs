using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
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
        public IServiceCollection AddServiceBus(Action<ServiceBusBuilder>? builder = null)
        {
            var serviceBusBuilder = new ServiceBusBuilder(Guard.AgainstNull(services));

            builder?.Invoke(serviceBusBuilder);

            services.TryAddSingleton<IEnvironmentService, EnvironmentService>();
            services.TryAddSingleton<IProcessService, ProcessService>();
            services.TryAddSingleton<ISerializer, JsonSerializer>();
            services.TryAddSingleton<IServiceBusPolicy, DefaultServiceBusPolicy>();
            services.TryAddSingleton<IMessageRouteProvider, MessageRouteProvider>();
            services.TryAddSingleton<IIdentityProvider, DefaultIdentityProvider>();
            services.TryAddScoped<IMessageHandlerInvoker, MessageHandlerInvoker>();
            services.TryAddSingleton<IUriResolver, UriResolver>();
            services.TryAddSingleton<ITransportService, TransportService>();
            services.TryAddSingleton<ITransportFactoryService, TransportFactoryService>();
            services.TryAddSingleton<ISubscriptionService, SubscriptionService>();
            services.TryAddSingleton<ISubscriptionQuery, NullSubscriptionQuery>();
            services.TryAddSingleton<IEncryptionService, EncryptionService>();
            services.TryAddSingleton<ICompressionService, CompressionService>();
            services.TryAddSingleton<IServiceBusConfiguration, ServiceBusConfiguration>();
            services.TryAddSingleton<IMemoryCache, MemoryCache>();

            services.TryAddSingleton<IDeferredMessageProcessor, DeferredMessageProcessor>();

            services.TryAddKeyedSingleton<IProcessor>("DeferredMessageProcessor", (provider, _) => provider.GetRequiredService<IDeferredMessageProcessor>());
            services.TryAddKeyedScoped<IProcessor, InboxProcessor>("InboxProcessor");
            services.TryAddKeyedScoped<IProcessor, OutboxProcessor>("OutboxProcessor");

            services.AddThreading(threadingBuilder =>
            {
                threadingBuilder.ConfigureProcessorIdle("InboxProcessor", options =>
                {
                    options.Durations = serviceBusBuilder.Options.Inbox.IdleDurations.Any()
                        ? serviceBusBuilder.Options.Inbox.IdleDurations
                        : ServiceBusOptions.DefaultIdleDurations.ToList();
                });

                threadingBuilder.ConfigureProcessorIdle("OutboxProcessor", options =>
                {
                    options.Durations = serviceBusBuilder.Options.Outbox.IdleDurations.Any()
                        ? serviceBusBuilder.Options.Outbox.IdleDurations
                        : ServiceBusOptions.DefaultIdleDurations.ToList();
                });

                threadingBuilder.ConfigureProcessorIdle("DeferredMessageProcessor", options =>
                {
                    options.Durations = [serviceBusBuilder.Options.Inbox.DeferredMessageProcessorIdleDuration];
                });
            });

            if (!serviceBusBuilder.ShouldSuppressPipelineProcessing)
            {
                services.AddPipelines(pipelineBuilder =>
                {
                    pipelineBuilder.Options.ReusePipelines = false;

                    pipelineBuilder.AddAssembly(typeof(ServiceBus).Assembly);

                    pipelineBuilder.Options.UseTransactionScope<InboxMessagePipeline>("Handle");

                    serviceBusBuilder.OnAddPipelines(pipelineBuilder);
                });
            }

            var transactionScopeFactoryType = typeof(ITransactionScopeFactory);

            if (services.All(item => item.ServiceType != transactionScopeFactoryType))
            {
                services.AddTransactionScope();
            }

            services.AddOptions<ServiceBusOptions>().Configure(options =>
            {
                options.Inbox = serviceBusBuilder.Options.Inbox;
                options.Outbox = serviceBusBuilder.Options.Outbox;

                ApplyDefaults(options.Inbox);
                ApplyDefaults(options.Outbox);

                options.AddMessageHandlers = serviceBusBuilder.Options.AddMessageHandlers;
                options.CacheIdentity = serviceBusBuilder.Options.CacheIdentity;
                options.CompressionAlgorithm = serviceBusBuilder.Options.CompressionAlgorithm;
                options.CreatePhysicalTransports = serviceBusBuilder.Options.CreatePhysicalTransports;
                options.EncryptionAlgorithm = serviceBusBuilder.Options.EncryptionAlgorithm;
                options.RemoveCorruptMessages = serviceBusBuilder.Options.RemoveCorruptMessages;

                options.UriMappings = serviceBusBuilder.Options.UriMappings;
                options.MessageRoutes = serviceBusBuilder.Options.MessageRoutes;
                options.Subscription = serviceBusBuilder.Options.Subscription;
            });

            services.AddSingleton<IMessageHandlerDelegateRegistry>(_ => new MessageHandlerDelegateRegistry(serviceBusBuilder.GetMessageHandlerDelegates()));
            services.AddSingleton<IDirectMessageHandlerDelegateRegistry>(_ => new DirectMessageHandlerDelegateRegistry(serviceBusBuilder.GetDirectMessageHandlerDelegates()));

            if (serviceBusBuilder.Options.AddMessageHandlers)
            {
                foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
                {
                    serviceBusBuilder.AddMessageHandlers(assembly);
                }
            }
            else
            {
                serviceBusBuilder.AddMessageHandlers(typeof(ServiceBus).Assembly);
            }

            services.AddSingleton<IMessageSender, MessageSender>();
            services.AddSingleton<IServiceBus, ServiceBus>();

            if (!serviceBusBuilder.ShouldSuppressHostedService)
            {
                services.AddHostedService<ServiceBusHostedService>();
            }

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
                processorOptions.IgnoreOnFailureDurations = [..ServiceBusOptions.DefaultIgnoreOnFailureDurations];
            }

            if (!processorOptions.IdleDurations.Any())
            {
                processorOptions.IdleDurations = [..ServiceBusOptions.DefaultIdleDurations];
            }
        }
    }
}