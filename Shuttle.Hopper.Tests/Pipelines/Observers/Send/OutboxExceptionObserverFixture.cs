using Moq;
using NUnit.Framework;
using Shuttle.Core.Pipelines;
using Shuttle.Core.Serialization;

namespace Shuttle.Hopper.Tests;

[TestFixture]
public class OutboxExceptionObserverFixture
{
    [Test]
    public async Task Should_be_able_to_skip_when_exception_has_been_handled_async()
    {
        var busPolicy = new Mock<IBusPolicy>();
        var serializer = new Mock<ISerializer>();
        var workTransport = new Mock<ITransport>();
        var errorTransport = new Mock<ITransport>();

        var observer = new OutboxExceptionObserver(busPolicy.Object, serializer.Object);

        var pipeline = Pipeline.Get()
            .AddObserver(new ThrowExceptionObserver())
            .AddObserver(new HandleExceptionObserver()) // marks exception as handled
            .AddObserver(observer);

        pipeline
            .AddStage(".")
            .WithEvent<OnException>();

        pipeline.State.SetWorkTransport(workTransport.Object);
        pipeline.State.SetErrorTransport(errorTransport.Object);

        await pipeline.ExecuteAsync();

        Assert.That(pipeline.Aborted, Is.True);

        busPolicy.VerifyNoOtherCalls();
        serializer.VerifyNoOtherCalls();
        workTransport.VerifyNoOtherCalls();
        errorTransport.VerifyNoOtherCalls();
    }

    [Test]
    public async Task Should_be_able_to_skip_when_there_is_no_message_available_async()
    {
        var busPolicy = new Mock<IBusPolicy>();
        var serializer = new Mock<ISerializer>();
        var workTransport = new Mock<ITransport>();
        var errorTransport = new Mock<ITransport>();

        var observer = new OutboxExceptionObserver(busPolicy.Object, serializer.Object);

        var pipeline = Pipeline.Get()
            .AddObserver(new ThrowExceptionObserver())
            .AddObserver(observer);

        pipeline
            .AddStage(".")
            .WithEvent<OnException>();

        pipeline.State.SetWorkTransport(workTransport.Object);
        pipeline.State.SetErrorTransport(errorTransport.Object);

        await pipeline.ExecuteAsync();

        Assert.That(pipeline.Aborted, Is.True);

        busPolicy.VerifyNoOtherCalls();
        serializer.VerifyNoOtherCalls();
        workTransport.VerifyNoOtherCalls();
        errorTransport.VerifyNoOtherCalls();
    }

    [Test]
    public async Task Should_be_able_to_release_when_there_is_no_transport_message_available_async()
    {
        var busPolicy = new Mock<IBusPolicy>();
        var serializer = new Mock<ISerializer>();
        var workTransport = new Mock<ITransport>();
        var errorTransport = new Mock<ITransport>();

        var observer = new OutboxExceptionObserver(busPolicy.Object, serializer.Object);

        var pipeline = Pipeline.Get()
            .AddObserver(new ThrowExceptionObserver())
            .AddObserver(observer);

        pipeline
            .AddStage(".")
            .WithEvent<OnException>();

        var receivedMessage = new ReceivedMessage(Stream.Null, Guid.NewGuid());

        pipeline.State.SetReceivedMessage(receivedMessage);
        pipeline.State.SetWorkTransport(workTransport.Object);
        pipeline.State.SetErrorTransport(errorTransport.Object);

        await pipeline.ExecuteAsync();

        workTransport.Verify(m => m.ReleaseAsync(receivedMessage.AcknowledgementToken, It.IsAny<CancellationToken>()), Times.Once);

        Assert.That(pipeline.Aborted, Is.True);

        busPolicy.VerifyNoOtherCalls();
        serializer.VerifyNoOtherCalls();
        workTransport.VerifyNoOtherCalls();
        errorTransport.VerifyNoOtherCalls();
    }

    [Test]
    public async Task Should_be_able_to_release_when_using_a_stream_async()
    {
        var busPolicy = new Mock<IBusPolicy>();
        var serializer = new Mock<ISerializer>();
        var workTransport = new Mock<ITransport>();
        var errorTransport = new Mock<ITransport>();

        var observer = new OutboxExceptionObserver(busPolicy.Object, serializer.Object);

        var pipeline = Pipeline.Get()
            .AddObserver(new ThrowExceptionObserver())
            .AddObserver(observer);

        pipeline
            .AddStage(".")
            .WithEvent<OnException>();

        workTransport.Setup(m => m.Type).Returns(TransportType.Stream);

        var receivedMessage = new ReceivedMessage(Stream.Null, Guid.NewGuid());

        pipeline.State.SetTransportMessage(new());
        pipeline.State.SetReceivedMessage(receivedMessage);
        pipeline.State.SetWorkTransport(workTransport.Object);
        pipeline.State.SetErrorTransport(errorTransport.Object);

        await pipeline.ExecuteAsync();

        workTransport.Verify(m => m.ReleaseAsync(receivedMessage.AcknowledgementToken, It.IsAny<CancellationToken>()), Times.Once);

        workTransport.Verify(m => m.Type, Times.Once);

        Assert.That(pipeline.Aborted, Is.True);

        busPolicy.VerifyNoOtherCalls();
        serializer.VerifyNoOtherCalls();
        workTransport.VerifyNoOtherCalls();
        errorTransport.VerifyNoOtherCalls();
    }

    [Test]
    public async Task Should_be_able_to_retry_async()
    {
        var busPolicy = new Mock<IBusPolicy>();
        var serializer = new Mock<ISerializer>();
        var workTransport = new Mock<ITransport>();
        var errorTransport = new Mock<ITransport>();

        var observer = new OutboxExceptionObserver(busPolicy.Object, serializer.Object);

        var pipeline = Pipeline.Get()
            .AddObserver(new ThrowExceptionObserver())
            .AddObserver(observer);

        pipeline
            .AddStage(".")
            .WithEvent<OnException>();

        workTransport.Setup(m => m.Type).Returns(TransportType.Queue);
        busPolicy.Setup(m => m.EvaluateOutboxFailure(It.IsAny<IPipelineContext<PipelineFailed>>()))
            .Returns(new MessageFailureAction(true, TimeSpan.Zero));

        var transportMessage = new TransportMessage();
        var receivedMessage = new ReceivedMessage(Stream.Null, Guid.NewGuid());

        pipeline.State.SetTransportMessage(transportMessage);
        pipeline.State.SetReceivedMessage(receivedMessage);
        pipeline.State.SetWorkTransport(workTransport.Object);
        pipeline.State.SetErrorTransport(errorTransport.Object);

        await pipeline.ExecuteAsync();

        serializer.Verify(m => m.SerializeAsync(transportMessage, It.IsAny<CancellationToken>()));
        workTransport.Verify(m => m.SendAsync(transportMessage, It.IsAny<Stream>(), It.IsAny<CancellationToken>()), Times.Once);
        workTransport.Verify(m => m.AcknowledgeAsync(receivedMessage.AcknowledgementToken, It.IsAny<CancellationToken>()), Times.Once);

        busPolicy.Verify(m => m.EvaluateOutboxFailure(It.IsAny<IPipelineContext<PipelineFailed>>()), Times.Once);
        workTransport.Verify(m => m.Type, Times.Once);

        Assert.That(pipeline.Aborted, Is.True);

        busPolicy.VerifyNoOtherCalls();
        serializer.VerifyNoOtherCalls();
        workTransport.VerifyNoOtherCalls();
        errorTransport.VerifyNoOtherCalls();
    }

    [Test]
    public async Task Should_be_able_to_not_retry_async()
    {
        var busPolicy = new Mock<IBusPolicy>();
        var serializer = new Mock<ISerializer>();
        var workTransport = new Mock<ITransport>();
        var errorTransport = new Mock<ITransport>();

        var observer = new OutboxExceptionObserver(busPolicy.Object, serializer.Object);

        var pipeline = Pipeline.Get()
            .AddObserver(new ThrowExceptionObserver())
            .AddObserver(observer);

        pipeline
            .AddStage(".")
            .WithEvent<OnException>();

        workTransport.Setup(m => m.Type).Returns(TransportType.Queue);
        busPolicy.Setup(m => m.EvaluateOutboxFailure(It.IsAny<IPipelineContext<PipelineFailed>>())).Returns(new MessageFailureAction(false, TimeSpan.Zero));

        var transportMessage = new TransportMessage();
        var receivedMessage = new ReceivedMessage(Stream.Null, Guid.NewGuid());

        pipeline.State.SetTransportMessage(transportMessage);
        pipeline.State.SetReceivedMessage(receivedMessage);
        pipeline.State.SetWorkTransport(workTransport.Object);
        pipeline.State.SetErrorTransport(errorTransport.Object);

        await pipeline.ExecuteAsync();

        serializer.Verify(m => m.SerializeAsync(transportMessage, It.IsAny<CancellationToken>()));
        errorTransport.Verify(m => m.SendAsync(transportMessage, It.IsAny<Stream>(), It.IsAny<CancellationToken>()), Times.Once);
        workTransport.Verify(m => m.AcknowledgeAsync(receivedMessage.AcknowledgementToken, It.IsAny<CancellationToken>()), Times.Once);

        busPolicy.Verify(m => m.EvaluateOutboxFailure(It.IsAny<IPipelineContext<PipelineFailed>>()), Times.Once);
        workTransport.Verify(m => m.Type, Times.Once);

        Assert.That(pipeline.Aborted, Is.True);

        busPolicy.VerifyNoOtherCalls();
        serializer.VerifyNoOtherCalls();
        workTransport.VerifyNoOtherCalls();
        errorTransport.VerifyNoOtherCalls();
    }

    [Test]
    public async Task Should_be_able_to_not_retry_with_no_error_transport_async()
    {
        var busPolicy = new Mock<IBusPolicy>();
        var serializer = new Mock<ISerializer>();
        var workTransport = new Mock<ITransport>();

        var observer = new OutboxExceptionObserver(busPolicy.Object, serializer.Object);

        var pipeline = Pipeline.Get()
            .AddObserver(new ThrowExceptionObserver())
            .AddObserver(observer);

        pipeline
            .AddStage(".")
            .WithEvent<OnException>();

        workTransport.Setup(m => m.Type).Returns(TransportType.Queue);
        busPolicy.Setup(m => m.EvaluateOutboxFailure(It.IsAny<IPipelineContext<PipelineFailed>>())).Returns(new MessageFailureAction(false, TimeSpan.Zero));

        var transportMessage = new TransportMessage();
        var receivedMessage = new ReceivedMessage(Stream.Null, Guid.NewGuid());

        pipeline.State.SetTransportMessage(transportMessage);
        pipeline.State.SetReceivedMessage(receivedMessage);
        pipeline.State.SetWorkTransport(workTransport.Object);

        await pipeline.ExecuteAsync();

        serializer.Verify(m => m.SerializeAsync(transportMessage, It.IsAny<CancellationToken>()));
        workTransport.Verify(m => m.SendAsync(transportMessage, It.IsAny<Stream>(), It.IsAny<CancellationToken>()), Times.Once);
        workTransport.Verify(m => m.AcknowledgeAsync(receivedMessage.AcknowledgementToken, It.IsAny<CancellationToken>()), Times.Once);

        busPolicy.Verify(m => m.EvaluateOutboxFailure(It.IsAny<IPipelineContext<PipelineFailed>>()), Times.Once);
        workTransport.Verify(m => m.Type, Times.Once);

        Assert.That(pipeline.Aborted, Is.True);

        busPolicy.VerifyNoOtherCalls();
        serializer.VerifyNoOtherCalls();
        workTransport.VerifyNoOtherCalls();
    }
}