using System.Collections.Concurrent;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using Shuttle.Pipelines;
using Shuttle.Threading;

namespace Shuttle.Hopper.Tests;

[TestFixture]
public class BusControlFixture
{
    private const int ThreadCount = 3;

    [Test]
    public async Task Should_expose_the_inbox_and_outbox_configuration_while_started()
    {
        await using var serviceProvider = BuildServiceProvider(_ => { });

        var busControl = (BusControl)serviceProvider.GetRequiredService<IBusControl>();
        var busConfiguration = serviceProvider.GetRequiredService<IBusConfiguration>();

        await busControl.StartAsync();

        Assert.Multiple(() =>
        {
            Assert.That(busControl.Inbox, Is.SameAs(busConfiguration.Inbox));
            Assert.That(busControl.Inbox, Is.Not.Null);
            Assert.That(busControl.Outbox, Is.SameAs(busConfiguration.Outbox));
            Assert.That(busControl.Outbox, Is.Not.Null);
        });

        await busControl.StopAsync();

        Assert.Throws<ApplicationException>(() => _ = busControl.Inbox);
    }

    [Test]
    public async Task Should_stop_the_thread_pools_that_were_started_when_startup_fails_part_way()
    {
        var stoppedThreads = new ConcurrentBag<string>();

        await using var serviceProvider = BuildServiceProvider(services =>
        {
            services.Configure<PipelineOptions>(options =>
            {
                options.StageStarting += async (args, _) =>
                {
                    await Task.CompletedTask;

                    // The "Final" stage runs after the thread pools have been started.
                    if (args.Pipeline is IStartupPipeline && args.Pipeline.StageName.Equals("Final"))
                    {
                        throw new InvalidOperationException("[simulated startup failure]");
                    }
                };
            });

            services.Configure<ThreadingOptions>(options =>
            {
                options.ProcessorThreadStopped += async (args, _) =>
                {
                    stoppedThreads.Add(args.ProcessorThread.ServiceKey);

                    await Task.CompletedTask;
                };
            });
        });

        var busControl = serviceProvider.GetRequiredService<IBusControl>();

        Assert.ThrowsAsync(Is.InstanceOf<Exception>(), async () => await busControl.StartAsync());

        Assert.Multiple(() =>
        {
            Assert.That(busControl.Started, Is.False);
            Assert.That(stoppedThreads.Count(serviceKey => serviceKey.Equals("InboxProcessor")), Is.EqualTo(ThreadCount), "Not every inbox thread was stopped.");
            Assert.That(stoppedThreads.Count(serviceKey => serviceKey.Equals("InboxProcessor:priority")), Is.EqualTo(ThreadCount), "Not every additional inbox thread was stopped.");
            Assert.That(stoppedThreads.Count(serviceKey => serviceKey.Equals("OutboxProcessor")), Is.EqualTo(ThreadCount), "Not every outbox thread was stopped.");
            Assert.That(stoppedThreads, Does.Contain("DeferredMessageProcessor"), "The deferred message thread was not stopped.");
        });
    }

    private static ServiceProvider BuildServiceProvider(Action<IServiceCollection> configureServices)
    {
        var services = new ServiceCollection();

        services
            .AddHopper(options =>
            {
                options.Inbox.WorkTransportUri = new("resilience://resilience/work");
                options.Inbox.DeferredTransportUri = new("resilience://resilience/deferred");
                options.Inbox.ErrorTransportUri = new("resilience://resilience/error");
                options.Inbox.ThreadCount = ThreadCount;
                options.Inbox.IdleDurations = [TimeSpan.FromMilliseconds(10)];

                options.Outbox.WorkTransportUri = new("resilience://resilience/outbox-work");
                options.Outbox.ThreadCount = ThreadCount;
                options.Outbox.IdleDurations = [TimeSpan.FromMilliseconds(10)];

                options.AutoStart = false;
            })
            .AddInbox("priority", options =>
            {
                options.WorkTransportUri = new("resilience://resilience/work-priority");
                options.ThreadCount = ThreadCount;
                options.IdleDurations = [TimeSpan.FromMilliseconds(10)];
            });

        services
            .AddSingleton<ResilienceTransportFactory>()
            .AddSingleton<ITransportFactory>(serviceProvider => serviceProvider.GetRequiredService<ResilienceTransportFactory>());

        configureServices(services);

        return services.BuildServiceProvider();
    }
}
