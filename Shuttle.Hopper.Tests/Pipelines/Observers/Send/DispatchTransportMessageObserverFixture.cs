using Moq;
using NUnit.Framework;
using Shuttle.Core.Pipelines;

namespace Shuttle.Hopper.Tests;

[TestFixture]
public class DispatchTransportMessageObserverFixture
{
    [Test]
    public async Task Should_be_able_to_dispatch_message_to_outbox_async()
    {
        var serviceBusConfiguration = new Mock<IServiceBusConfiguration>();
        var transportService = new Mock<ITransportService>();
        var outboxConfiguration = new Mock<IOutboxConfiguration>();
        var recipientTransport = new Mock<ITransport>();
        var outboxTransport = new Mock<ITransport>();
        var transportMessage = new TransportMessage
        {
            RecipientInboxWorkTransportUri = "transport://recipient/work-transport"
        };
        var transportMessageStream = Stream.Null;

        outboxConfiguration.Setup(m => m.WorkTransport).Returns(outboxTransport.Object);
        serviceBusConfiguration.Setup(m => m.Outbox).Returns(outboxConfiguration.Object);
        transportService.Setup(m => m.GetAsync(new(transportMessage.RecipientInboxWorkTransportUri), It.IsAny<CancellationToken>())).ReturnsAsync(recipientTransport.Object);

        var observer = new DispatchTransportMessageObserver(serviceBusConfiguration.Object, transportService.Object);

        var pipeline = new Pipeline(PipelineDependencies.Empty())
            .AddObserver(observer);

        pipeline
            .AddStage(".")
            .WithEvent<DispatchTransportMessage>();

        pipeline.State.SetTransportMessage(transportMessage);
        pipeline.State.SetTransportMessageStream(transportMessageStream);

        await pipeline.ExecuteAsync();

        outboxTransport.Verify(m => m.SendAsync(transportMessage, It.IsAny<Stream>(), It.IsAny<CancellationToken>()), Times.Once);

        serviceBusConfiguration.VerifyNoOtherCalls();
        transportService.VerifyNoOtherCalls();
        outboxConfiguration.VerifyNoOtherCalls();
        recipientTransport.VerifyNoOtherCalls();
        outboxTransport.VerifyNoOtherCalls();
    }

    [Test]
    public async Task Should_be_able_to_dispatch_message_to_recipient_async()
    {
        var serviceBusConfiguration = new Mock<IServiceBusConfiguration>();
        var transportService = new Mock<ITransportService>();
        var recipientTransport = new Mock<ITransport>();
        var outboxTransport = new Mock<ITransport>();
        var transportMessage = new TransportMessage
        {
            RecipientInboxWorkTransportUri = "transport://recipient/work-transport"
        };
        var transportMessageStream = Stream.Null;

        transportService.Setup(m => m.GetAsync(It.IsAny<Uri>(), It.IsAny<CancellationToken>())).ReturnsAsync(() => recipientTransport.Object);

        var observer = new DispatchTransportMessageObserver(serviceBusConfiguration.Object, transportService.Object);

        var pipeline = new Pipeline(PipelineDependencies.Empty())
            .AddObserver(observer);

        pipeline
            .AddStage(".")
            .WithEvent<DispatchTransportMessage>();

        pipeline.State.SetTransportMessage(transportMessage);
        pipeline.State.SetTransportMessageStream(transportMessageStream);

        await pipeline.ExecuteAsync();

        transportService.Verify(m => m.GetAsync(It.IsAny<Uri>(), It.IsAny<CancellationToken>()), Times.Once);
        recipientTransport.Verify(m => m.SendAsync(transportMessage, It.IsAny<Stream>(), It.IsAny<CancellationToken>()), Times.Once);

        transportService.VerifyNoOtherCalls();
        recipientTransport.VerifyNoOtherCalls();
        outboxTransport.VerifyNoOtherCalls();
    }
}