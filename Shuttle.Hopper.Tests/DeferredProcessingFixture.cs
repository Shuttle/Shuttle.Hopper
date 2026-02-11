using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace Shuttle.Hopper.Tests;

[TestFixture]
public class DeferredProcessingFixture
{
    [Test]
    public async Task Should_be_able_to_defer_processing()
    {
        var messagesReturned = new List<TransportMessage>();

        var serviceProvider = new ServiceCollection()
            .AddHopper(builder =>
            {
                builder.Options.Inbox = new()
                {
                    WorkTransportUri = new("memory://memory/work-transport"),
                    DeferredTransportUri = new("memory://memory/deferred-transport"),
                    ErrorTransportUri = new("memory://memory/error-transport"),
                    DeferredMessageProcessorResetInterval = TimeSpan.FromMilliseconds(500)
                };

                builder.Options.DeferredMessageProcessingHalted += async (_, _) =>
                {
                    Console.WriteLine(@"[deferred processing halted]");

                    await Task.CompletedTask;
                };

                builder.Options.MessageReturned += async (e, _) =>
                {
                    messagesReturned.Add(e.TransportMessage);

                    await Task.CompletedTask;
                };
            })
            .AddSingleton<ITransportFactory, MemoryTransportFactory>()
            .BuildServiceProvider();

        await using var busControl = await serviceProvider.GetRequiredService<IBusControl>().StartAsync();
        
        var bus = serviceProvider.GetRequiredService<IBus>();

        await bus.SendAsync(new SimpleCommand(), builder => builder.ToSelf().DeferUntil(DateTimeOffset.UtcNow.Add(TimeSpan.FromSeconds(1))));
        await bus.SendAsync(new SimpleCommand(), builder => builder.ToSelf().DeferUntil(DateTimeOffset.UtcNow.Add(TimeSpan.FromSeconds(2))));
        await bus.SendAsync(new SimpleCommand(), builder => builder.ToSelf().DeferUntil(DateTimeOffset.UtcNow.Add(TimeSpan.FromSeconds(3))));
        
        var timeout = DateTimeOffset.UtcNow.AddMilliseconds(3500);

        while (messagesReturned.Count < 3 && DateTimeOffset.UtcNow < timeout)
        {
            Thread.Sleep(250);
        }
    }
}