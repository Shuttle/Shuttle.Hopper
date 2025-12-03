using Microsoft.Extensions.Options;
using Moq;
using NUnit.Framework;
using Shuttle.Core.Pipelines;

namespace Shuttle.Hopper.Tests;

[TestFixture]
public class DeferTransportMessageObserverFixture
{
    [Test]
    public async Task Should_be_able_to_return_when_message_does_not_need_to_be_deferred_async()
    {
        var deferredMessageProcessor = new Mock<IDeferredMessageProcessor>();
        var workTransport = new Mock<ITransport>();
        var deferredTransport = new Mock<ITransport>();

        var observer = new DeferTransportMessageObserver(Options.Create(new ServiceBusOptions()), deferredMessageProcessor.Object);

        var pipeline = new Pipeline(Options.Create(new PipelineOptions()), new Mock<IServiceProvider>().Object)
            .AddObserver(observer);

        pipeline
            .AddStage(".")
            .WithEvent<OnAfterDeserializeTransportMessage>();

        pipeline.State.SetTransportMessage(new());
        pipeline.State.SetWorkTransport(workTransport.Object);
        pipeline.State.SetDeferredTransport(deferredTransport.Object);

        workTransport.SetupSequence(m => m.Type)
            .Returns(TransportType.Stream)
            .Returns(TransportType.Queue);

        await pipeline.ExecuteAsync();

        Assert.That(pipeline.Aborted, Is.False);

        workTransport.VerifyNoOtherCalls();
        deferredTransport.VerifyNoOtherCalls();
        deferredMessageProcessor.VerifyNoOtherCalls();

        await pipeline.ExecuteAsync();

        Assert.That(pipeline.Aborted, Is.False);

        workTransport.VerifyNoOtherCalls();
        deferredTransport.VerifyNoOtherCalls();
        deferredMessageProcessor.VerifyNoOtherCalls();
    }

    [Test]
    public async Task Should_be_able_to_defer_message_to_work_transport_async()
    {
        var deferredMessageProcessor = new Mock<IDeferredMessageProcessor>();
        var workTransport = new Mock<ITransport>();

        var observer = new DeferTransportMessageObserver(Options.Create(new ServiceBusOptions()), deferredMessageProcessor.Object);

        var pipeline = new Pipeline(Options.Create(new PipelineOptions()), new Mock<IServiceProvider>().Object)
            .AddObserver(observer);

        pipeline
            .AddStage(".")
            .WithEvent<OnAfterDeserializeTransportMessage>();

        var transportMessage = new TransportMessage { IgnoreTillDate = DateTime.Now.AddDays(1) };
        var receivedMessage = new ReceivedMessage(new MemoryStream(), Guid.NewGuid());

        pipeline.State.SetTransportMessage(transportMessage);
        pipeline.State.SetReceivedMessage(receivedMessage);
        pipeline.State.SetWorkTransport(workTransport.Object);

        workTransport.Setup(m => m.Type).Returns(TransportType.Queue);

        await pipeline.ExecuteAsync();

        workTransport.Verify(m => m.SendAsync(transportMessage, It.IsAny<Stream>(), CancellationToken.None), Times.Once);
        workTransport.Verify(m => m.AcknowledgeAsync(receivedMessage.AcknowledgementToken, CancellationToken.None), Times.Once);

        workTransport.Verify(m => m.Type, Times.Once);

        Assert.That(pipeline.Aborted, Is.True);

        workTransport.VerifyNoOtherCalls();
        deferredMessageProcessor.VerifyNoOtherCalls();
    }

    [Test]
    public async Task Should_be_able_to_defer_message_to_deferred_transport_async()
    {
        var deferredMessageProcessor = new Mock<IDeferredMessageProcessor>();
        var workTransport = new Mock<ITransport>();
        var deferredTransport = new Mock<ITransport>();

        var observer = new DeferTransportMessageObserver(Options.Create(new ServiceBusOptions()), deferredMessageProcessor.Object);

        var pipeline = new Pipeline(Options.Create(new PipelineOptions()), new Mock<IServiceProvider>().Object)
            .AddObserver(observer);

        pipeline
            .AddStage(".")
            .WithEvent<OnAfterDeserializeTransportMessage>();

        var transportMessage = new TransportMessage { IgnoreTillDate = DateTime.Now.AddDays(1) };
        var receivedMessage = new ReceivedMessage(new MemoryStream(), Guid.NewGuid());

        pipeline.State.SetTransportMessage(transportMessage);
        pipeline.State.SetReceivedMessage(receivedMessage);
        pipeline.State.SetWorkTransport(workTransport.Object);
        pipeline.State.SetDeferredTransport(deferredTransport.Object);

        await pipeline.ExecuteAsync();

        deferredTransport.Verify(m => m.SendAsync(transportMessage, It.IsAny<Stream>(), CancellationToken.None), Times.Once);
        workTransport.Verify(m => m.AcknowledgeAsync(receivedMessage.AcknowledgementToken, CancellationToken.None), Times.Once);
        deferredMessageProcessor.Verify(m => m.MessageDeferredAsync(It.IsAny<DateTime>()), Times.Once);

        workTransport.Verify(m => m.Type, Times.Once);

        Assert.That(pipeline.Aborted, Is.True);

        workTransport.VerifyNoOtherCalls();
        deferredTransport.VerifyNoOtherCalls();
        deferredMessageProcessor.VerifyNoOtherCalls();
    }
}