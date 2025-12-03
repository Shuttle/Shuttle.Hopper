using Microsoft.Extensions.Options;
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
        var serviceBusPolicy = new Mock<IServiceBusPolicy>();
        var serializer = new Mock<ISerializer>();
        var workTransport = new Mock<ITransport>();
        var errorTransport = new Mock<ITransport>();

        var observer = new OutboxExceptionObserver(serviceBusPolicy.Object, serializer.Object);

        var pipeline = new Pipeline(Options.Create(new PipelineOptions()), new Mock<IServiceProvider>().Object)
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

        serviceBusPolicy.VerifyNoOtherCalls();
        serializer.VerifyNoOtherCalls();
        workTransport.VerifyNoOtherCalls();
        errorTransport.VerifyNoOtherCalls();
    }

    [Test]
    public async Task Should_be_able_to_skip_when_there_is_no_message_available_async()
    {
        var serviceBusPolicy = new Mock<IServiceBusPolicy>();
        var serializer = new Mock<ISerializer>();
        var workTransport = new Mock<ITransport>();
        var errorTransport = new Mock<ITransport>();

        var observer = new OutboxExceptionObserver(serviceBusPolicy.Object, serializer.Object);

        var pipeline = new Pipeline(Options.Create(new PipelineOptions()), new Mock<IServiceProvider>().Object)
            .AddObserver(new ThrowExceptionObserver())
            .AddObserver(observer);

        pipeline
            .AddStage(".")
            .WithEvent<OnException>();

        pipeline.State.SetWorkTransport(workTransport.Object);
        pipeline.State.SetErrorTransport(errorTransport.Object);

        await pipeline.ExecuteAsync();

        Assert.That(pipeline.Aborted, Is.True);

        serviceBusPolicy.VerifyNoOtherCalls();
        serializer.VerifyNoOtherCalls();
        workTransport.VerifyNoOtherCalls();
        errorTransport.VerifyNoOtherCalls();
    }

    [Test]
    public async Task Should_be_able_to_release_when_there_is_no_transport_message_available_async()
    {
        var serviceBusPolicy = new Mock<IServiceBusPolicy>();
        var serializer = new Mock<ISerializer>();
        var workTransport = new Mock<ITransport>();
        var errorTransport = new Mock<ITransport>();

        var observer = new OutboxExceptionObserver(serviceBusPolicy.Object, serializer.Object);

        var pipeline = new Pipeline(Options.Create(new PipelineOptions()), new Mock<IServiceProvider>().Object)
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

        workTransport.Verify(m => m.ReleaseAsync(receivedMessage.AcknowledgementToken, CancellationToken.None), Times.Once);

        Assert.That(pipeline.Aborted, Is.True);

        serviceBusPolicy.VerifyNoOtherCalls();
        serializer.VerifyNoOtherCalls();
        workTransport.VerifyNoOtherCalls();
        errorTransport.VerifyNoOtherCalls();
    }

    [Test]
    public async Task Should_be_able_to_release_when_using_a_stream_async()
    {
        var serviceBusPolicy = new Mock<IServiceBusPolicy>();
        var serializer = new Mock<ISerializer>();
        var workTransport = new Mock<ITransport>();
        var errorTransport = new Mock<ITransport>();

        var observer = new OutboxExceptionObserver(serviceBusPolicy.Object, serializer.Object);

        var pipeline = new Pipeline(Options.Create(new PipelineOptions()), new Mock<IServiceProvider>().Object)
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

        workTransport.Verify(m => m.ReleaseAsync(receivedMessage.AcknowledgementToken, CancellationToken.None), Times.Once);

        workTransport.Verify(m => m.Type, Times.Once);

        Assert.That(pipeline.Aborted, Is.True);

        serviceBusPolicy.VerifyNoOtherCalls();
        serializer.VerifyNoOtherCalls();
        workTransport.VerifyNoOtherCalls();
        errorTransport.VerifyNoOtherCalls();
    }

    [Test]
    public async Task Should_be_able_to_retry_async()
    {
        var serviceBusPolicy = new Mock<IServiceBusPolicy>();
        var serializer = new Mock<ISerializer>();
        var workTransport = new Mock<ITransport>();
        var errorTransport = new Mock<ITransport>();

        var observer = new OutboxExceptionObserver(serviceBusPolicy.Object, serializer.Object);

        var pipeline = new Pipeline(Options.Create(new PipelineOptions()), new Mock<IServiceProvider>().Object)
            .AddObserver(new ThrowExceptionObserver())
            .AddObserver(observer);

        pipeline
            .AddStage(".")
            .WithEvent<OnException>();

        workTransport.Setup(m => m.Type).Returns(TransportType.Queue);
        serviceBusPolicy.Setup(m => m.EvaluateOutboxFailure(It.IsAny<IPipelineContext<OnPipelineException>>()))
            .Returns(new MessageFailureAction(true, TimeSpan.Zero));

        var transportMessage = new TransportMessage();
        var receivedMessage = new ReceivedMessage(Stream.Null, Guid.NewGuid());

        pipeline.State.SetTransportMessage(transportMessage);
        pipeline.State.SetReceivedMessage(receivedMessage);
        pipeline.State.SetWorkTransport(workTransport.Object);
        pipeline.State.SetErrorTransport(errorTransport.Object);

        await pipeline.ExecuteAsync();

        serializer.Verify(m => m.SerializeAsync(transportMessage));
        workTransport.Verify(m => m.SendAsync(transportMessage, It.IsAny<Stream>(), CancellationToken.None), Times.Once);
        workTransport.Verify(m => m.AcknowledgeAsync(receivedMessage.AcknowledgementToken, CancellationToken.None), Times.Once);

        serviceBusPolicy.Verify(m => m.EvaluateOutboxFailure(It.IsAny<IPipelineContext<OnPipelineException>>()), Times.Once);
        workTransport.Verify(m => m.Type, Times.Once);

        Assert.That(pipeline.Aborted, Is.True);

        serviceBusPolicy.VerifyNoOtherCalls();
        serializer.VerifyNoOtherCalls();
        workTransport.VerifyNoOtherCalls();
        errorTransport.VerifyNoOtherCalls();
    }

    [Test]
    public async Task Should_be_able_to_not_retry_async()
    {
        var serviceBusPolicy = new Mock<IServiceBusPolicy>();
        var serializer = new Mock<ISerializer>();
        var workTransport = new Mock<ITransport>();
        var errorTransport = new Mock<ITransport>();

        var observer = new OutboxExceptionObserver(serviceBusPolicy.Object, serializer.Object);

        var pipeline = new Pipeline(Options.Create(new PipelineOptions()), new Mock<IServiceProvider>().Object)
            .AddObserver(new ThrowExceptionObserver())
            .AddObserver(observer);

        pipeline
            .AddStage(".")
            .WithEvent<OnException>();

        workTransport.Setup(m => m.Type).Returns(TransportType.Queue);
        serviceBusPolicy.Setup(m => m.EvaluateOutboxFailure(It.IsAny<IPipelineContext<OnPipelineException>>())).Returns(new MessageFailureAction(false, TimeSpan.Zero));

        var transportMessage = new TransportMessage();
        var receivedMessage = new ReceivedMessage(Stream.Null, Guid.NewGuid());

        pipeline.State.SetTransportMessage(transportMessage);
        pipeline.State.SetReceivedMessage(receivedMessage);
        pipeline.State.SetWorkTransport(workTransport.Object);
        pipeline.State.SetErrorTransport(errorTransport.Object);

        await pipeline.ExecuteAsync();

        serializer.Verify(m => m.SerializeAsync(transportMessage));
        errorTransport.Verify(m => m.SendAsync(transportMessage, It.IsAny<Stream>(), CancellationToken.None), Times.Once);
        workTransport.Verify(m => m.AcknowledgeAsync(receivedMessage.AcknowledgementToken, CancellationToken.None), Times.Once);

        serviceBusPolicy.Verify(m => m.EvaluateOutboxFailure(It.IsAny<IPipelineContext<OnPipelineException>>()), Times.Once);
        workTransport.Verify(m => m.Type, Times.Once);

        Assert.That(pipeline.Aborted, Is.True);

        serviceBusPolicy.VerifyNoOtherCalls();
        serializer.VerifyNoOtherCalls();
        workTransport.VerifyNoOtherCalls();
        errorTransport.VerifyNoOtherCalls();
    }

    [Test]
    public async Task Should_be_able_to_not_retry_with_no_error_transport_async()
    {
        var serviceBusPolicy = new Mock<IServiceBusPolicy>();
        var serializer = new Mock<ISerializer>();
        var workTransport = new Mock<ITransport>();

        var observer = new OutboxExceptionObserver(serviceBusPolicy.Object, serializer.Object);

        var pipeline = new Pipeline(Options.Create(new PipelineOptions()), new Mock<IServiceProvider>().Object)
            .AddObserver(new ThrowExceptionObserver())
            .AddObserver(observer);

        pipeline
            .AddStage(".")
            .WithEvent<OnException>();

        workTransport.Setup(m => m.Type).Returns(TransportType.Queue);
        serviceBusPolicy.Setup(m => m.EvaluateOutboxFailure(It.IsAny<IPipelineContext<OnPipelineException>>())).Returns(new MessageFailureAction(false, TimeSpan.Zero));

        var transportMessage = new TransportMessage();
        var receivedMessage = new ReceivedMessage(Stream.Null, Guid.NewGuid());

        pipeline.State.SetTransportMessage(transportMessage);
        pipeline.State.SetReceivedMessage(receivedMessage);
        pipeline.State.SetWorkTransport(workTransport.Object);

        await pipeline.ExecuteAsync();

        serializer.Verify(m => m.SerializeAsync(transportMessage));
        workTransport.Verify(m => m.SendAsync(transportMessage, It.IsAny<Stream>(), CancellationToken.None), Times.Once);
        workTransport.Verify(m => m.AcknowledgeAsync(receivedMessage.AcknowledgementToken, CancellationToken.None), Times.Once);

        serviceBusPolicy.Verify(m => m.EvaluateOutboxFailure(It.IsAny<IPipelineContext<OnPipelineException>>()), Times.Once);
        workTransport.Verify(m => m.Type, Times.Once);

        Assert.That(pipeline.Aborted, Is.True);

        serviceBusPolicy.VerifyNoOtherCalls();
        serializer.VerifyNoOtherCalls();
        workTransport.VerifyNoOtherCalls();
    }
}