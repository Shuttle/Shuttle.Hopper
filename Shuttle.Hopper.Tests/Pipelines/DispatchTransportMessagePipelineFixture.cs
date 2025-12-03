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

        transportService.Setup(m => m.GetAsync(recipientInboxWorkTransportUri, CancellationToken.None)).ReturnsAsync(new Mock<ITransport>().Object);

        var services = new ServiceCollection();

        services.AddSingleton(transportService.Object);

        services.AddServiceBus();

        var serviceProvider = services.BuildServiceProvider();

        var pipelineFactory = serviceProvider.GetRequiredService<IPipelineFactory>();

        var transportMessage = new TransportMessage
        {
            Message = [],
            RecipientInboxWorkTransportUri = recipientInboxWorkTransportUri.ToString()
        };

        var sw = new Stopwatch();

        sw.Start();

        var count = 0;

        while (sw.ElapsedMilliseconds < 1000)
        {
            var pipeline = await pipelineFactory.GetPipelineAsync<DispatchTransportMessagePipeline>();

            pipeline.State.Replace(StateKeys.TransportMessage, transportMessage);

            await pipeline.ExecuteAsync().ConfigureAwait(false);

            await pipelineFactory.ReleasePipelineAsync(pipeline);

            count++;
        }

        sw.Stop();

        Console.WriteLine($@"[message-dispatch] : count = {count} / ms = {sw.ElapsedMilliseconds}");

        Assert.That(count, Is.GreaterThan(1000));
    }
}