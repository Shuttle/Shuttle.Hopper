using Microsoft.Extensions.Options;
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
        var serviceBus = new Mock<IServiceBus>();
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
        serviceBus.Setup(m => m.Outbox).Returns(outboxConfiguration.Object);
        transportService.Setup(m => m.GetAsync(new(transportMessage.RecipientInboxWorkTransportUri), CancellationToken.None)).ReturnsAsync(recipientTransport.Object);

        var observer = new DispatchTransportMessageObserver(serviceBus.Object, transportService.Object);

        var pipeline = new Pipeline(Options.Create(new PipelineOptions()), new Mock<IServiceProvider>().Object)
            .AddObserver(observer);

        pipeline
            .AddStage(".")
            .WithEvent<OnDispatchTransportMessage>();

        pipeline.State.SetTransportMessage(transportMessage);
        pipeline.State.SetTransportMessageStream(transportMessageStream);

        await pipeline.ExecuteAsync();

        outboxTransport.Verify(m => m.SendAsync(transportMessage, It.IsAny<Stream>(), CancellationToken.None), Times.Once);

        serviceBus.VerifyNoOtherCalls();
        transportService.VerifyNoOtherCalls();
        outboxConfiguration.VerifyNoOtherCalls();
        recipientTransport.VerifyNoOtherCalls();
        outboxTransport.VerifyNoOtherCalls();
    }

    [Test]
    public async Task Should_be_able_to_dispatch_message_to_recipient_async()
    {
        var serviceBus = new Mock<IServiceBus>();
        var transportService = new Mock<ITransportService>();
        var recipientTransport = new Mock<ITransport>();
        var outboxTransport = new Mock<ITransport>();
        var transportMessage = new TransportMessage
        {
            RecipientInboxWorkTransportUri = "transport://recipient/work-transport"
        };
        var transportMessageStream = Stream.Null;

        transportService.Setup(m => m.GetAsync(It.IsAny<Uri>(), CancellationToken.None)).ReturnsAsync(() => recipientTransport.Object);

        var observer = new DispatchTransportMessageObserver(serviceBus.Object, transportService.Object);

        var pipeline = new Pipeline(Options.Create(new PipelineOptions()), new Mock<IServiceProvider>().Object)
            .AddObserver(observer);

        pipeline
            .AddStage(".")
            .WithEvent<OnDispatchTransportMessage>();

        pipeline.State.SetTransportMessage(transportMessage);
        pipeline.State.SetTransportMessageStream(transportMessageStream);

        await pipeline.ExecuteAsync();

        transportService.Verify(m => m.GetAsync(It.IsAny<Uri>(), CancellationToken.None), Times.Once);
        recipientTransport.Verify(m => m.SendAsync(transportMessage, It.IsAny<Stream>(), CancellationToken.None), Times.Once);

        transportService.VerifyNoOtherCalls();
        recipientTransport.VerifyNoOtherCalls();
        outboxTransport.VerifyNoOtherCalls();
    }
}