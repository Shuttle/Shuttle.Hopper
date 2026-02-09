using Moq;
using NUnit.Framework;
using Shuttle.Core.Pipelines;
using Shuttle.Core.Serialization;

namespace Shuttle.Hopper.Tests;

[TestFixture]
public class ReceiveExceptionObserverFixture : IPipelineObserver<OnException>
{
    public async Task ExecuteAsync(IPipelineContext<OnException> pipelineContext, CancellationToken cancellationToken = default)
    {
        await Task.CompletedTask;

        throw new(string.Empty, new UnrecoverableHandlerException());
    }

    [Test]
    public async Task Should_be_able_to_move_message_to_error_transport_when_UnrecoverableHandlerException_is_thrown_async()
    {
        var policy = new Mock<IBusPolicy>();

        policy.Setup(m => m.EvaluateMessageHandlingFailure(It.IsAny<IPipelineContext<PipelineFailed>>()))
            .Returns(new MessageFailureAction(true, TimeSpan.Zero));

        var workTransport = new Mock<ITransport>();
        var errorTransport = new Mock<ITransport>();

        errorTransport.Setup(m => m.Uri).Returns(new TransportUri("transport://configuration/some-transport"));

        var observer = new ReceivePipelineFailedObserver(policy.Object,
            new Mock<ISerializer>().Object);

        var pipeline = Pipeline.Get()
            .AddObserver(this)
            .AddObserver(observer);

        pipeline
            .AddStage(".")
            .WithEvent<OnException>();

        var transportMessage = new TransportMessage();
        var receivedMessage = new ReceivedMessage(Stream.Null, Guid.NewGuid());

        pipeline.State.Add(StateKeys.ReceivedMessage, receivedMessage);
        pipeline.State.Add(StateKeys.TransportMessage, transportMessage);
        pipeline.State.Add(StateKeys.WorkTransport, workTransport.Object);
        pipeline.State.Add(StateKeys.ErrorTransport, errorTransport.Object);

        await pipeline.ExecuteAsync(CancellationToken.None);

        workTransport.Verify(m => m.AcknowledgeAsync(receivedMessage.AcknowledgementToken, It.IsAny<CancellationToken>()), Times.Once);
        errorTransport.Verify(m => m.SendAsync(transportMessage, It.IsAny<Stream>(), It.IsAny<CancellationToken>()), Times.Once);
    }
}