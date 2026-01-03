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

        var observer = new DeferTransportMessageObserver(Options.Create(new HopperOptions()), deferredMessageProcessor.Object);

        var pipeline = new Pipeline(PipelineDependencies.Empty())
            .AddObserver(observer);

        pipeline
            .AddStage(".")
            .WithEvent<TransportMessageDeserialized>();

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

        var observer = new DeferTransportMessageObserver(Options.Create(new HopperOptions()), deferredMessageProcessor.Object);

        var pipeline = new Pipeline(PipelineDependencies.Empty())
            .AddObserver(observer);

        pipeline
            .AddStage(".")
            .WithEvent<TransportMessageDeserialized>();

        var transportMessage = new TransportMessage { IgnoreUntil = DateTimeOffset.UtcNow.AddDays(1) };
        var receivedMessage = new ReceivedMessage(new MemoryStream(), Guid.NewGuid());

        pipeline.State.SetTransportMessage(transportMessage);
        pipeline.State.SetReceivedMessage(receivedMessage);
        pipeline.State.SetWorkTransport(workTransport.Object);

        workTransport.Setup(m => m.Type).Returns(TransportType.Queue);

        await pipeline.ExecuteAsync();

        workTransport.Verify(m => m.SendAsync(transportMessage, It.IsAny<Stream>(), It.IsAny<CancellationToken>()), Times.Once);
        workTransport.Verify(m => m.AcknowledgeAsync(receivedMessage.AcknowledgementToken, It.IsAny<CancellationToken>()), Times.Once);

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

        var observer = new DeferTransportMessageObserver(Options.Create(new HopperOptions()), deferredMessageProcessor.Object);

        var pipeline = new Pipeline(PipelineDependencies.Empty())
            .AddObserver(observer);

        pipeline
            .AddStage(".")
            .WithEvent<TransportMessageDeserialized>();

        var transportMessage = new TransportMessage { IgnoreUntil = DateTimeOffset.Now.AddDays(1) };
        var receivedMessage = new ReceivedMessage(new MemoryStream(), Guid.NewGuid());

        pipeline.State.SetTransportMessage(transportMessage);
        pipeline.State.SetReceivedMessage(receivedMessage);
        pipeline.State.SetWorkTransport(workTransport.Object);
        pipeline.State.SetDeferredTransport(deferredTransport.Object);

        await pipeline.ExecuteAsync();

        deferredTransport.Verify(m => m.SendAsync(transportMessage, It.IsAny<Stream>(), It.IsAny<CancellationToken>()), Times.Once);
        workTransport.Verify(m => m.AcknowledgeAsync(receivedMessage.AcknowledgementToken, It.IsAny<CancellationToken>()), Times.Once);
        deferredMessageProcessor.Verify(m => m.MessageDeferredAsync(It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()), Times.Once);

        workTransport.Verify(m => m.Type, Times.Once);

        Assert.That(pipeline.Aborted, Is.True);

        workTransport.VerifyNoOtherCalls();
        deferredTransport.VerifyNoOtherCalls();
        deferredMessageProcessor.VerifyNoOtherCalls();
    }
}