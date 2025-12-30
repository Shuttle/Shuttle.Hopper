using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using NUnit.Framework;

namespace Shuttle.Hopper.Tests;

[TestFixture]
public class ServiceBusPerformanceFixture
{
    [Test]
    public async Task Should_be_able_to_send_messages_with_optimal_performance_async()
    {
        var services = new ServiceCollection();

        var messageRouteProvider = new Mock<IMessageRouteProvider>();

        messageRouteProvider.Setup(m => m.GetRouteUrisAsync(It.IsAny<string>())).Returns(Task.FromResult<IEnumerable<string>>(new List<string> { "null-transport://null/null" }));

        services.AddSingleton(messageRouteProvider.Object);
        services.AddSingleton<ITransportFactory, NullTransportFactory>();

        services.AddHopperx();

        var serviceProvider = services.BuildServiceProvider();

        var count = 0;

        await using var serviceBus = await serviceProvider.GetRequiredService<IServiceBus>().StartAsync();

        var sw = new Stopwatch();

        sw.Start();

        while (sw.ElapsedMilliseconds < 1000)
        {
            await serviceBus.SendAsync(new SimpleCommand($"{Guid.NewGuid()}"));

            count++;
        }

        sw.Stop();

        Console.WriteLine($@"[service-bus-send] : count = {count} / ms = {sw.ElapsedMilliseconds}");

        Assert.That(count, Is.GreaterThan(500));
    }
}