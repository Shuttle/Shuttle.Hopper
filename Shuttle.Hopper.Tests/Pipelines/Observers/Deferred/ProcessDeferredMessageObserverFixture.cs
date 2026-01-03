using Microsoft.Extensions.Options;
using Moq;
using NUnit.Framework;
using Shuttle.Core.Pipelines;

namespace Shuttle.Hopper.Tests;

[TestFixture]
public class ProcessDeferredMessageObserverFixture
{
    [Test]
    public async Task Should_be_able_to_process_a_deferred_message_when_ready_async()
    {
        var observer = new ProcessDeferredMessageObserver(Options.Create(new HopperOptions()));

        var pipeline = new Pipeline(PipelineDependencies.Empty()).AddObserver(observer);

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

        Assert.That(pipeline.State.GetDeferredMessageReturned, Is.False);

        deferredTransport.Verify(m => m.ReleaseAsync(It.IsAny<object>(), It.IsAny<CancellationToken>()), Times.Once);

        deferredTransport.VerifyNoOtherCalls();
        workTransport.VerifyNoOtherCalls();

        await Task.Delay(TimeSpan.FromMilliseconds(200));

        workTransport = new();
        deferredTransport = new();

        pipeline.State.Clear();
        pipeline.State.SetWorkTransport(workTransport.Object);
        pipeline.State.SetDeferredTransport(deferredTransport.Object);
        pipeline.State.SetReceivedMessage(receivedMessage);
        pipeline.State.SetTransportMessage(transportMessage);

        await pipeline.ExecuteAsync();

        Assert.That(pipeline.State.GetDeferredMessageReturned, Is.True);

        deferredTransport.Verify(m => m.AcknowledgeAsync(It.IsAny<object>(), It.IsAny<CancellationToken>()), Times.Once);
        workTransport.Verify(m => m.SendAsync(transportMessage, It.IsAny<Stream>(), It.IsAny<CancellationToken>()), Times.Once);

        deferredTransport.VerifyNoOtherCalls();
        workTransport.VerifyNoOtherCalls();
    }
}