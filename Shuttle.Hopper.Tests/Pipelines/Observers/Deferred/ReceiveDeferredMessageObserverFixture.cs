using Microsoft.Extensions.Options;
using Moq;
using NUnit.Framework;
using Shuttle.Core.Pipelines;

namespace Shuttle.Hopper.Tests;

[TestFixture]
public class ReceiveDeferredMessageObserverFixture
{
    [Test]
    public async Task Should_be_able_to_get_a_message_from_the_deferred_transport_when_available_async()
    {
        var observer = new ReceiveDeferredMessageObserver();

        var pipeline = Pipeline.Get().AddObserver(observer);

        pipeline
            .AddStage(".")
            .WithEvent<ReceiveMessage>();

        var deferredTransport = new Mock<ITransport>();
        var receivedMessage = new ReceivedMessage(new MemoryStream(), Guid.NewGuid());

        deferredTransport.SetupSequence(m => m.ReceiveAsync(CancellationToken.None))
            .Returns(Task.FromResult<ReceivedMessage?>(receivedMessage))
            .Returns(Task.FromResult<ReceivedMessage?>(null));

        pipeline.State.SetDeferredTransport(deferredTransport.Object);

        await pipeline.ExecuteAsync();

        Assert.That(pipeline.State.GetReceivedMessage(), Is.Not.Null);
        Assert.That(pipeline.State.GetWorkPerformed(), Is.True);
        Assert.That(pipeline.Aborted, Is.False);

        pipeline.State.Clear();
        pipeline.State.SetDeferredTransport(deferredTransport.Object);

        await pipeline.ExecuteAsync();

        Assert.That(pipeline.State.GetReceivedMessage(), Is.Null);
        Assert.That(pipeline.State.GetWorkPerformed(), Is.False);
        Assert.That(pipeline.Aborted, Is.True);
    }
}