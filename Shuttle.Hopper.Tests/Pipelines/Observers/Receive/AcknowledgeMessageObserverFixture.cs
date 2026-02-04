using Moq;
using NUnit.Framework;
using Shuttle.Core.Pipelines;

namespace Shuttle.Hopper.Tests;

[TestFixture]
public class AcknowledgeMessageObserverFixture
{
    [Test]
    public async Task Should_be_able_to_ignore_acknowledgement_on_failures_async()
    {
        var observer = new AcknowledgeMessageObserver();

        var pipeline = Pipeline.Get()
            .AddObserver(new ThrowExceptionObserver())
            .AddObserver(new HandleExceptionObserver())
            .AddObserver(observer);

        pipeline
            .AddStage(".")
            .WithEvent<OnException>()
            .WithEvent<MessageAcknowledged>();

        var workTransport = new Mock<ITransport>();

        pipeline.State.SetWorkTransport(workTransport.Object);

        await pipeline.ExecuteAsync();

        workTransport.VerifyNoOtherCalls();
    }

    [Test]
    public async Task Should_be_able_to_acknowledge_message_async()
    {
        var observer = new AcknowledgeMessageObserver();

        var pipeline = Pipeline.Get()
            .AddObserver(observer);

        pipeline
            .AddStage(".")
            .WithEvent<MessageAcknowledged>();

        var workTransport = new Mock<ITransport>();
        var receivedMessage = new ReceivedMessage(new Mock<Stream>().Object, Guid.NewGuid());

        pipeline.State.SetWorkTransport(workTransport.Object);
        pipeline.State.SetReceivedMessage(receivedMessage);

        await pipeline.ExecuteAsync();

        workTransport.Verify(m => m.AcknowledgeAsync(receivedMessage.AcknowledgementToken, It.IsAny<CancellationToken>()), Times.Once);

        workTransport.VerifyNoOtherCalls();
    }
}