using Microsoft.Extensions.Options;
using Moq;
using NUnit.Framework;
using Shuttle.Core.Pipelines;
using Shuttle.Core.Serialization;

namespace Shuttle.Hopper.Tests;

[TestFixture]
public class DeserializeMessageObserverFixture
{
    [Test]
    public void Should_throw_exception_on_invariant_failure_async()
    {
        var serializer = new Mock<ISerializer>();

        var observer = new DeserializeMessageObserver(Options.Create(new HopperOptions()), serializer.Object);

        var pipeline = Pipeline.Get()
            .AddObserver(observer);

        pipeline
            .AddStage(".")
            .WithEvent<DeserializeMessage>();

        var exception = Assert.ThrowsAsync<Core.Pipelines.PipelineException>(() => pipeline.ExecuteAsync())!;

        Assert.That(exception, Is.Not.Null);
        Assert.That(exception.InnerException?.Message, Contains.Substring(StateKeys.TransportMessage));

        serializer.VerifyNoOtherCalls();
    }

    [Test]
    public async Task Should_be_able_to_deserialize_message_async()
    {
        var serializer = new Mock<ISerializer>();

        var observer = new DeserializeMessageObserver(Options.Create(new HopperOptions()), serializer.Object);

        var pipeline = Pipeline.Get()
            .AddObserver(observer);

        pipeline
            .AddStage(".")
            .WithEvent<DeserializeMessage>();

        pipeline.State.SetTransportMessage(new()
        {
            AssemblyQualifiedName = typeof(TransportMessage).AssemblyQualifiedName!,
            Message = []
        });

        await pipeline.ExecuteAsync();

        serializer.Verify(m => m.DeserializeAsync(typeof(TransportMessage), It.IsAny<Stream>(), It.IsAny<CancellationToken>()), Times.Once);

        serializer.VerifyNoOtherCalls();
    }

    [Test]
    public async Task Should_be_able_to_retransport_on_exception_async()
    {
        var serializer = new Mock<ISerializer>();
        var workTransport = new Mock<ITransport>();
        var errorTransport = new Mock<ITransport>();
        var messageDeserializationExceptionCount = 0;

        var serviceBusOptions = new HopperOptions();

        serviceBusOptions.MessageDeserializationException += async (_, _) =>
        {
            messageDeserializationExceptionCount++;

            await Task.CompletedTask;
        };

        var observer = new DeserializeMessageObserver(Options.Create(serviceBusOptions), serializer.Object);

        var pipeline = Pipeline.Get()
            .AddObserver(observer);

        pipeline
            .AddStage(".")
            .WithEvent<DeserializeMessage>();

        var transportMessageType = typeof(TransportMessage);
        var receivedMessage = new ReceivedMessage(Stream.Null, Guid.NewGuid());
        var transportMessage = new TransportMessage
        {
            AssemblyQualifiedName = transportMessageType.AssemblyQualifiedName!,
            Message = []
        };

        pipeline.State.SetTransportMessage(transportMessage);

        pipeline.State.SetWorkTransport(workTransport.Object);
        pipeline.State.SetErrorTransport(errorTransport.Object);
        pipeline.State.SetReceivedMessage(receivedMessage);

        serializer.Setup(m => m.DeserializeAsync(transportMessageType, It.IsAny<Stream>(), It.IsAny<CancellationToken>())).Throws<Exception>();

        workTransport.Setup(m => m.Type).Returns(TransportType.Queue);

        await pipeline.ExecuteAsync();

        serializer.Verify(m => m.DeserializeAsync(typeof(TransportMessage), It.IsAny<Stream>(), It.IsAny<CancellationToken>()), Times.Once);
        serializer.Verify(m => m.SerializeAsync(It.IsAny<object>(), It.IsAny<CancellationToken>()), Times.Once);
        errorTransport.Verify(m => m.SendAsync(transportMessage, It.IsAny<Stream>(), It.IsAny<CancellationToken>()), Times.Once);
        workTransport.Verify(m => m.AcknowledgeAsync(receivedMessage.AcknowledgementToken, It.IsAny<CancellationToken>()), Times.Once);

        workTransport.Verify(m => m.Type, Times.Once);

        Assert.That(messageDeserializationExceptionCount, Is.EqualTo(1));

        serializer.VerifyNoOtherCalls();
        errorTransport.VerifyNoOtherCalls();
        workTransport.VerifyNoOtherCalls();
    }
}