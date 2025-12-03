using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Moq;
using NUnit.Framework;
using Shuttle.Core.Pipelines;
using Shuttle.Core.System;
using Shuttle.Core.Threading;
using JsonSerializer = Shuttle.Core.Serialization.JsonSerializer;

namespace Shuttle.Hopper.Tests;

[TestFixture]
public class DeferredProcessingFixture
{
    [Test]
    public async Task Should_be_able_to_defer_processing()
    {
        var serializer = new JsonSerializer(Options.Create(new JsonSerializerOptions()));
        var pipelineFactory = new Mock<IPipelineFactory>();
        var configuration = new Mock<IServiceBusConfiguration>();
        var serviceScopeFactory = new Mock<IServiceScopeFactory>();

        serviceScopeFactory.Setup(m => m.CreateScope()).Returns(new Mock<IServiceScope>().Object);

        var serviceBusOptions = new ServiceBusOptions
        {
            Inbox = new()
            {
                DeferredTransportUri = "memory://memory/deferred-transport",
                DeferredMessageProcessorResetInterval = TimeSpan.FromMilliseconds(500)
            }
        };

        serviceBusOptions.DeferredMessageProcessingHalted += async (_, _) =>
        {
            Console.WriteLine(@"[deferred processing halted]");

            await Task.CompletedTask;
        };

        var messagesReturned = new List<TransportMessage>();

        serviceBusOptions.MessageReturned += async (e, _) =>
        {
            messagesReturned.Add(e.TransportMessage);

            await Task.CompletedTask;
        };
        
        var serviceBusOptionsWrapped = Options.Create(serviceBusOptions);
        var pipelineOptionsWrapped = Options.Create(new PipelineOptions());

        var inboxConfiguration = new InboxConfiguration
        {
            WorkTransport = new MemoryTransport(serviceBusOptions, new("memory://memory/work-transport")),
            DeferredTransport = new MemoryTransport(serviceBusOptions, new("memory://memory/deferred-transport")),
            ErrorTransport = new MemoryTransport(serviceBusOptions, new("memory://memory/error-transport"))
        };

        configuration.Setup(m => m.Inbox).Returns(inboxConfiguration);

        var processDeferredMessageObserver = new ProcessDeferredMessageObserver(serviceBusOptionsWrapped);

        pipelineFactory.Setup(m => m.GetPipelineAsync<DeferredMessagePipeline>(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DeferredMessagePipeline(pipelineOptionsWrapped, new Mock<IServiceProvider>().Object, configuration.Object, new GetDeferredMessageObserver(), new DeserializeTransportMessageObserver(serviceBusOptionsWrapped, serializer, new EnvironmentService(), new ProcessService()), processDeferredMessageObserver));

        var deferredMessageProcessor = new DeferredMessageProcessor(serviceBusOptionsWrapped, pipelineFactory.Object);

        var transportMessage1 = CreateTransportMessage(DateTime.Now.AddSeconds(3).ToUniversalTime());
        var transportMessage2 = CreateTransportMessage(DateTime.Now.AddSeconds(2).ToUniversalTime());
        var 
            
            transportMessage3 = CreateTransportMessage(DateTime.Now.AddSeconds(1).ToUniversalTime());

        await inboxConfiguration.DeferredTransport.SendAsync(transportMessage1, await serializer.SerializeAsync(transportMessage1));
        await inboxConfiguration.DeferredTransport.SendAsync(transportMessage2, await serializer.SerializeAsync(transportMessage2));
        await inboxConfiguration.DeferredTransport.SendAsync(transportMessage3, await serializer.SerializeAsync(transportMessage3));

        var timeout = DateTime.Now.AddMilliseconds(3500);

        await new ProcessorThreadPool("DeferredMessageProcessor", 1, serviceScopeFactory.Object, new DeferredMessageProcessorFactory(deferredMessageProcessor), new()).StartAsync();

        while (messagesReturned.Count < 3 && DateTime.Now < timeout)
        {
            Thread.Sleep(250);
        }

        Assert.That(messagesReturned.Find(item => item.MessageId.Equals(transportMessage1.MessageId)), Is.Not.Null);
        Assert.That(messagesReturned.Find(item => item.MessageId.Equals(transportMessage2.MessageId)), Is.Not.Null);
        Assert.That(messagesReturned.Find(item => item.MessageId.Equals(transportMessage3.MessageId)), Is.Not.Null);
    }

    private static TransportMessage CreateTransportMessage(DateTime ignoreTillDate)
    {
        return new()
        {
            MessageId = new("973808b9-8cc6-433b-b9d2-a08e1236c104"),
            PrincipalIdentityName = "unit-test",
            MessageType = "message-type",
            AssemblyQualifiedName = "assembly-qualified-name",
            IgnoreTillDate = ignoreTillDate
        };
    }
}