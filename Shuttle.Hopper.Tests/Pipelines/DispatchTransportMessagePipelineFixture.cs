using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using NUnit.Framework;
using Shuttle.Core.Pipelines;

namespace Shuttle.Hopper.Tests;

[TestFixture]
public class DispatchTransportMessagePipelineFixture
{
    [Test]
    public async Task Should_be_able_to_execute_dispatch_transport_message_pipeline_with_optimal_performance_async()
    {
        var recipientInboxWorkTransportUri = new Uri("transport://null/null");

        var transportService = new Mock<ITransportService>();

        transportService.Setup(m => m.GetAsync(recipientInboxWorkTransportUri, It.IsAny<CancellationToken>())).ReturnsAsync(new Mock<ITransport>().Object);

        var services = new ServiceCollection();

        services.AddSingleton(transportService.Object);

        services.AddServiceBus();

        var serviceProvider = services.BuildServiceProvider();

        var serviceBus = serviceProvider.GetRequiredService<IServiceBus>();
        var pipelineFactory = serviceProvider.GetRequiredService<IPipelineFactory>();

        var transportMessage = new TransportMessage
        {
            Message = [],
            RecipientInboxWorkTransportUri = recipientInboxWorkTransportUri.ToString()
        };

        var sw = new Stopwatch();
        var count = 0;

        await using (await serviceBus.StartAsync())
        {
            sw.Start();

            while (sw.ElapsedMilliseconds < 1000)
            {
                var pipeline = await pipelineFactory.GetPipelineAsync<DispatchTransportMessagePipeline>();

                pipeline.State.Replace(StateKeys.TransportMessage, transportMessage);

                await pipeline.ExecuteAsync().ConfigureAwait(false);

                count++;
            }

            sw.Stop();
        }

        Console.WriteLine($@"[message-dispatch] : count = {count} / ms = {sw.ElapsedMilliseconds}");

        Assert.That(count, Is.GreaterThan(1000));
    }
}