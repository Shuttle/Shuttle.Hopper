using System.Collections.Concurrent;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using Shuttle.Reflection;
using Shuttle.Threading;

namespace Shuttle.Hopper.Tests;

[TestFixture]
public class AdditionalInboxFixture
{
    private const string DeferredUri = "resilience://resilience/deferred";
    private const string ErrorUri = "resilience://resilience/error";
    private const string PriorityErrorUri = "resilience://resilience/error-priority";
    private const string PriorityWorkUri = "resilience://resilience/work-priority";
    private const string WorkUri = "resilience://resilience/work";

    [Test]
    public async Task Should_process_messages_from_the_primary_and_additional_inbox()
    {
        const int messageCount = 10;

        var handled = new ConcurrentDictionary<Guid, ConcurrentBag<string>>();
        var serviceKeys = new ConcurrentDictionary<string, byte>();

        await using var context = await AdditionalInboxContext.StartAsync(_ => { }, async handlerContext =>
        {
            handled.GetOrAdd(handlerContext.Message.Id, _ => []).Add(handlerContext.State.GetWorkTransport()!.Uri.ToString());

            await Task.CompletedTask;
        }, threadingOptions =>
        {
            threadingOptions.ProcessorExecuting += async (args, _) =>
            {
                serviceKeys.TryAdd(args.ServiceKey, 0);

                await Task.CompletedTask;
            };
        });

        var expected = new Dictionary<Guid, string>();

        for (var i = 0; i < messageCount; i++)
        {
            expected.Add(await context.SendAsync(new(), WorkUri), WorkUri);
            expected.Add(await context.SendAsync(new(), PriorityWorkUri), PriorityWorkUri);
        }

        var completed = await context.WaitAsync(() => handled.Count >= expected.Count);

        Assert.Multiple(() =>
        {
            Assert.That(completed, Is.True, $"Handled {handled.Count} of {expected.Count} messages.");

            foreach (var (id, uri) in expected)
            {
                Assert.That(handled.TryGetValue(id, out var sources), Is.True, $"Message '{id}' was not handled.");
                Assert.That(sources, Is.EquivalentTo(new[] { uri }), $"Message '{id}' was not handled exactly once from '{uri}'.");
            }

            Assert.That(serviceKeys.Keys, Does.Contain("InboxProcessor"));
            Assert.That(serviceKeys.Keys, Does.Contain("InboxProcessor:priority"));
        });
    }

    [Test]
    public async Task Should_send_poison_messages_from_an_additional_inbox_to_the_primary_error_transport_when_none_is_configured()
    {
        const int messageCount = 3;

        await using var context = await AdditionalInboxContext.StartAsync(_ => { }, FailingHandlerAsync);

        for (var i = 0; i < messageCount; i++)
        {
            await context.SendAsync(new() { Fail = true }, PriorityWorkUri);
        }

        var completed = await context.WaitAsync(() => context.Transport(ErrorUri).SendCount >= messageCount);

        Assert.Multiple(() =>
        {
            Assert.That(completed, Is.True, "The poison messages did not reach the primary error transport.");
            Assert.That(context.Transport(PriorityWorkUri).Count, Is.Zero);
            Assert.That(context.Transport(PriorityWorkUri).UnacknowledgedCount, Is.Zero);
        });
    }

    [Test]
    public async Task Should_send_poison_messages_from_an_additional_inbox_to_its_own_error_transport_when_configured()
    {
        const int messageCount = 3;

        await using var context = await AdditionalInboxContext.StartAsync(options =>
        {
            options.AdditionalInboxes["priority"].ErrorTransportUri = new(PriorityErrorUri);
        }, FailingHandlerAsync);

        for (var i = 0; i < messageCount; i++)
        {
            await context.SendAsync(new() { Fail = true }, PriorityWorkUri);
        }

        var completed = await context.WaitAsync(() => context.Transport(PriorityErrorUri).SendCount >= messageCount);

        Assert.Multiple(() =>
        {
            Assert.That(completed, Is.True, "The poison messages did not reach the additional inbox's error transport.");
            Assert.That(context.Transport(ErrorUri).SendCount, Is.Zero);
        });
    }

    [Test]
    public async Task Should_return_deferred_messages_to_the_additional_inbox()
    {
        var handled = new ConcurrentDictionary<Guid, string>();

        await using var context = await AdditionalInboxContext.StartAsync(_ => { }, async handlerContext =>
        {
            handled.TryAdd(handlerContext.Message.Id, handlerContext.State.GetWorkTransport()!.Uri.ToString());

            await Task.CompletedTask;
        });

        var id = await context.SendAsync(new(), PriorityWorkUri, builder => builder.DeferFor(TimeSpan.FromMilliseconds(300)));

        var completed = await context.WaitAsync(() => handled.ContainsKey(id));

        Assert.Multiple(() =>
        {
            Assert.That(completed, Is.True, "The deferred message was never handled.");
            Assert.That(handled.GetValueOrDefault(id), Is.EqualTo(PriorityWorkUri));
            Assert.That(context.Transport(DeferredUri).SendCount, Is.GreaterThanOrEqualTo(1), "The message was never deferred.");
            Assert.That(context.Transport(WorkUri).SendCount, Is.Zero, "The deferred message was returned to the primary inbox.");
        });
    }

    [Test]
    public void Should_fail_to_start_when_an_additional_inbox_is_configured_but_not_registered()
    {
        AssertStartFails(options =>
        {
            options.AdditionalInboxes["unregistered"] = new() { WorkTransportUri = new("resilience://resilience/work-unregistered") };
        }, "unregistered");
    }

    [Test]
    public void Should_fail_to_start_when_an_additional_inbox_has_no_work_transport_uri()
    {
        AssertStartFails(options =>
        {
            options.AdditionalInboxes["priority"].WorkTransportUri = null;
        }, "'WorkTransportUri'");
    }

    [Test]
    public void Should_fail_to_start_when_an_additional_inbox_uses_a_duplicate_work_transport_uri()
    {
        AssertStartFails(options =>
        {
            options.AdditionalInboxes["priority"].WorkTransportUri = new(WorkUri);
        }, WorkUri);

        AssertStartFails(options =>
        {
            options.AdditionalInboxes["priority"].WorkTransportUri = new(DeferredUri);
        }, DeferredUri);

        AssertStartFails(options =>
        {
            options.AdditionalInboxes["priority"].WorkTransportUri = new(ErrorUri);
        }, ErrorUri);
    }

    [Test]
    public void Should_fail_to_start_when_an_additional_inbox_specifies_a_deferred_transport_uri()
    {
        AssertStartFails(options =>
        {
            options.AdditionalInboxes["priority"].DeferredTransportUri = new("resilience://resilience/deferred-priority");
        }, "'DeferredTransportUri'");
    }

    [Test]
    public void Should_fail_to_start_when_additional_inboxes_exist_without_a_primary_inbox()
    {
        AssertStartFails(options =>
        {
            options.Inbox.WorkTransportUri = null;
            options.Inbox.DeferredTransportUri = null;
            options.Inbox.ErrorTransportUri = null;
        }, "'Inbox.WorkTransportUri'");
    }

    private static void AssertStartFails(Action<HopperOptions> configureOptions, string expectedMessage)
    {
        var exception = Assert.ThrowsAsync(Is.InstanceOf<Exception>(), async () =>
        {
            await using var context = await AdditionalInboxContext.StartAsync(configureOptions, async _ => await Task.CompletedTask);
        });

        Assert.That(exception!.AllMessages(), Does.Contain(expectedMessage));
    }

    private static async Task FailingHandlerAsync(IHandlerContext<AdditionalInboxCommand> context)
    {
        await Task.CompletedTask;

        if (context.Message.Fail)
        {
            throw new InvalidOperationException($"[simulated failure] : id = '{context.Message.Id}'");
        }
    }

    private sealed class AdditionalInboxContext(ServiceProvider serviceProvider, IBusControl busControl, IBus bus, ResilienceTransportFactory transportFactory) : IAsyncDisposable
    {
        public static async Task<AdditionalInboxContext> StartAsync(Action<HopperOptions> configureOptions, Func<IHandlerContext<AdditionalInboxCommand>, Task> handler, Action<ThreadingOptions>? configureThreadingOptions = null)
        {
            var services = new ServiceCollection();

            services
                .AddHopper(options =>
                {
                    options.Inbox.WorkTransportUri = new(WorkUri);
                    options.Inbox.DeferredTransportUri = new(DeferredUri);
                    options.Inbox.ErrorTransportUri = new(ErrorUri);
                    options.Inbox.ThreadCount = 2;
                    options.Inbox.MaximumFailureCount = 1;
                    options.Inbox.IdleDurations = [TimeSpan.FromMilliseconds(10)];
                    options.Inbox.DeferredMessageProcessorIdleDuration = TimeSpan.FromMilliseconds(50);
                    options.Inbox.DeferredMessageProcessorResetInterval = TimeSpan.FromMilliseconds(250);

                    options.AdditionalInboxes["priority"] = new()
                    {
                        WorkTransportUri = new(PriorityWorkUri),
                        ThreadCount = 2,
                        MaximumFailureCount = 1,
                        IdleDurations = [TimeSpan.FromMilliseconds(10)]
                    };

                    configureOptions(options);
                })
                .AddInbox("priority")
                .AddMessageHandler(handler);

            services
                .AddSingleton<ResilienceTransportFactory>()
                .AddSingleton<ITransportFactory>(provider => provider.GetRequiredService<ResilienceTransportFactory>())
                .Configure<ThreadingOptions>(options => configureThreadingOptions?.Invoke(options));

            var provider = services.BuildServiceProvider();

            try
            {
                var control = await provider.GetRequiredService<IBusControl>().StartAsync();

                return new(provider, control, provider.GetRequiredService<IBus>(), provider.GetRequiredService<ResilienceTransportFactory>());
            }
            catch
            {
                await provider.DisposeAsync();
                throw;
            }
        }

        public ResilienceTransport Transport(string uri)
        {
            return transportFactory.Get(uri);
        }

        public async Task<Guid> SendAsync(AdditionalInboxCommand message, string recipientUri, Action<TransportMessageBuilder>? configure = null)
        {
            await bus.SendAsync(message, builder =>
            {
                builder.WithRecipient(recipientUri);

                configure?.Invoke(builder);
            });

            return message.Id;
        }

        public async Task<bool> WaitAsync(Func<bool> condition, int timeoutSeconds = 15)
        {
            var timeout = DateTimeOffset.UtcNow.AddSeconds(timeoutSeconds);

            while (DateTimeOffset.UtcNow < timeout)
            {
                if (condition())
                {
                    return true;
                }

                await Task.Delay(25);
            }

            return condition();
        }

        public async ValueTask DisposeAsync()
        {
            await busControl.DisposeAsync();
            await serviceProvider.DisposeAsync();
        }
    }
}
