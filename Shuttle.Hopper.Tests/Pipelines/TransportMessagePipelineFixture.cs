using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace Shuttle.Hopper.Tests;

[TestFixture]
public class TransportMessagePipelineFixture
{
    [Test]
    public async Task Should_be_able_execute_transport_message_pipeline_with_optimal_performance_async()
    {
        var services = new ServiceCollection();

        services.AddHopper();

        var serviceProvider = services.BuildServiceProvider();

        var bus = serviceProvider.GetRequiredService<IBusControl>();

        var sw = new Stopwatch();
        var count = 0;

        await using (await bus.StartAsync())
        {
            sw.Start();

            while (sw.ElapsedMilliseconds < 1000)
            {
                var pipeline = serviceProvider.GetRequiredService<ITransportMessagePipeline>();

                pipeline.State.Replace(StateKeys.Message, new());

                await pipeline.ExecuteAsync().ConfigureAwait(false);

                count++;
            }

            sw.Stop();
        }

        Console.WriteLine($@"[transport-message-assembly] : count = {count} / ms = {sw.ElapsedMilliseconds}");

        Assert.That(count, Is.GreaterThan(500));
    }
}