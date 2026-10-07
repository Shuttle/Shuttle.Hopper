using Microsoft.Extensions.Options;
using Moq;
using NUnit.Framework;
using Shuttle.Pipelines;

namespace Shuttle.Hopper.Tests;

[TestFixture]
public class ProcessDeferredMessageObserverFixture
{
    [Test]
    public async Task Should_be_able_to_process_a_deferred_message_when_ready_async()
    {
        var observer = new ProcessDeferredMessageObserver(Options.Create(new HopperOptions()));

        var pipeline = Pipeline.Get().AddObserver(observer);

        pipeline
            .AddStage(".")
            .WithEvent<ProcessDeferredMessage>();

        var workTransport = new Mock<ITransport>();
        var deferredTransport = new Mock<ITransport>();

        var transportMessage = new TransportMessage
        {
            IgnoreUntil = DateTimeOffset.Now.AddMilliseconds(200)
        };

        var receivedMessage = new ReceivedMessage(new MemoryStream(), Guid.NewGuid());

        pipeline.State.SetWorkTransport(workTransport.Object);
        pipeline.State.SetDeferredTransport(deferredTransport.Object);
        pipeline.State.SetReceivedMessage(receivedMessage);
        pipeline.State.SetTransportMessage(transportMessage);

        await pipeline.ExecuteAsync();

        Assert.That(pipeline.State.HasDeferredMessageReturned, Is.False);

        deferredTransport.Verify(m => m.ReleaseAsync(It.IsAny<object>(), pipeline, It.IsAny<CancellationToken>()), Times.Once);

        deferredTransport.VerifyNoOtherCalls();
        workTransport.VerifyNoOtherCalls();

        // Comfortably beyond the `IgnoreUntil` above; waiting for exactly the ignore duration is a coin flip given the
        // resolution of the system timer.
        await Task.Delay(TimeSpan.FromMilliseconds(500));

        workTransport = new();
        deferredTransport = new();

        pipeline.State.Clear();
        pipeline.State.SetWorkTransport(workTransport.Object);
        pipeline.State.SetDeferredTransport(deferredTransport.Object);
        pipeline.State.SetReceivedMessage(receivedMessage);
        pipeline.State.SetTransportMessage(transportMessage);

        await pipeline.ExecuteAsync();

        Assert.That(pipeline.State.HasDeferredMessageReturned, Is.True);

        deferredTransport.Verify(m => m.AcknowledgeAsync(It.IsAny<object>(), pipeline, It.IsAny<CancellationToken>()), Times.Once);
        workTransport.Verify(m => m.SendAsync(It.IsAny<Stream>(), pipeline, It.IsAny<CancellationToken>()), Times.Once);

        deferredTransport.VerifyNoOtherCalls();
        workTransport.VerifyNoOtherCalls();
    }

    [Test]
    public async Task Should_return_a_deferred_message_to_the_additional_inbox_addressed_by_the_recipient_uri()
    {
        const string priorityUri = "memory://memory/work-priority";

        var workTransport = new Mock<ITransport>();
        var deferredTransport = new Mock<ITransport>();
        var priorityTransport = new Mock<ITransport>();

        priorityTransport.Setup(m => m.Uri).Returns(new TransportUri(priorityUri));

        var pipeline = await ExecuteReadyDeferredMessageAsync(workTransport, deferredTransport, [priorityTransport.Object], priorityUri);

        Assert.That(pipeline.State.HasDeferredMessageReturned, Is.True);

        priorityTransport.Verify(m => m.SendAsync(It.IsAny<Stream>(), pipeline, It.IsAny<CancellationToken>()), Times.Once);
        deferredTransport.Verify(m => m.AcknowledgeAsync(It.IsAny<object>(), pipeline, It.IsAny<CancellationToken>()), Times.Once);

        workTransport.VerifyNoOtherCalls();
    }

    [TestCase("")]
    [TestCase("memory://memory/work-unknown")]
    public async Task Should_return_a_deferred_message_to_the_work_transport_when_the_recipient_is_not_an_additional_inbox(string recipientUri)
    {
        var workTransport = new Mock<ITransport>();
        var deferredTransport = new Mock<ITransport>();
        var priorityTransport = new Mock<ITransport>();

        priorityTransport.Setup(m => m.Uri).Returns(new TransportUri("memory://memory/work-priority"));

        var pipeline = await ExecuteReadyDeferredMessageAsync(workTransport, deferredTransport, [priorityTransport.Object], recipientUri);

        Assert.That(pipeline.State.HasDeferredMessageReturned, Is.True);

        workTransport.Verify(m => m.SendAsync(It.IsAny<Stream>(), pipeline, It.IsAny<CancellationToken>()), Times.Once);
        priorityTransport.Verify(m => m.SendAsync(It.IsAny<Stream>(), It.IsAny<IPipeline>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    private static async Task<IPipeline> ExecuteReadyDeferredMessageAsync(Mock<ITransport> workTransport, Mock<ITransport> deferredTransport, List<ITransport> additionalInboxWorkTransports, string recipientUri)
    {
        var observer = new ProcessDeferredMessageObserver(Options.Create(new HopperOptions()));

        var pipeline = Pipeline.Get().AddObserver(observer);

        pipeline
            .AddStage(".")
            .WithEvent<ProcessDeferredMessage>();

        pipeline.State.SetWorkTransport(workTransport.Object);
        pipeline.State.SetDeferredTransport(deferredTransport.Object);
        pipeline.State.SetAdditionalInboxWorkTransports(additionalInboxWorkTransports);
        pipeline.State.SetReceivedMessage(new(new MemoryStream(), Guid.NewGuid()));
        pipeline.State.SetTransportMessage(new()
        {
            RecipientInboxWorkTransportUri = recipientUri,
            IgnoreUntil = DateTimeOffset.UtcNow.AddSeconds(-1)
        });

        await pipeline.ExecuteAsync();

        return pipeline;
    }
}