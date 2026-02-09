using Microsoft.Extensions.Options;
using Moq;
using NUnit.Framework;
using Shuttle.Core.Pipelines;
using Shuttle.Core.Serialization;
using Shuttle.Core.System;

namespace Shuttle.Hopper.Tests;

public class DeserializeTransportMessageObserverFixture
{
    [Test]
    public async Task Should_be_able_to_acknowledge_message_when_corrupt_message_is_received_async()
    {
        var hopperOptions = Options.Create(new HopperOptions
        {
            RemoveCorruptMessages = true
        });
        var workTransport = new Mock<ITransport>();
        var errorTransport = new Mock<ITransport>();
        var serializer = new Mock<ISerializer>();
        var processService = new Mock<IProcessService>();
        var process = new Mock<IProcess>();

        workTransport.Setup(m => m.Uri).Returns(new TransportUri("transport://configuration/work-transport"));
        errorTransport.Setup(m => m.Uri).Returns(new TransportUri("transport://configuration/error-transport"));
        serializer.Setup(m => m.DeserializeAsync(It.IsAny<Type>(), It.IsAny<Stream>(), It.IsAny<CancellationToken>())).Throws<Exception>();
        processService.Setup(m => m.GetCurrentProcess()).Returns(process.Object);

        var observer = new DeserializeTransportMessageObserver(
            hopperOptions,
            serializer.Object,
            new Mock<IEnvironmentService>().Object,
            processService.Object);

        var pipeline = Pipeline.Get()
            .AddObserver(observer);

        pipeline
            .AddStage(".")
            .WithEvent<DeserializeTransportMessage>();

        var transportMessage = new TransportMessage();

        pipeline.State.SetReceivedMessage(new(Stream.Null, Guid.NewGuid()));
        pipeline.State.SetTransportMessage(transportMessage);
        pipeline.State.SetWorkTransport(workTransport.Object);
        pipeline.State.SetErrorTransport(errorTransport.Object);

        await pipeline.ExecuteAsync();

        process.Verify(m => m.Kill(), Times.Never);

        workTransport.Verify(m => m.AcknowledgeAsync(It.IsAny<object>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task Should_be_able_to_kill_process_when_corrupt_message_is_received_async()
    {
        var busOptions = Options.Create(new HopperOptions
        {
            RemoveCorruptMessages = false
        });

        var workTransport = new Mock<ITransport>();
        var serializer = new Mock<ISerializer>();
        var processService = new Mock<IProcessService>();
        var process = new Mock<IProcess>();

        workTransport.Setup(m => m.Uri).Returns(new TransportUri("transport://configuration/work-transport"));
        serializer.Setup(m => m.DeserializeAsync(It.IsAny<Type>(), It.IsAny<Stream>(), It.IsAny<CancellationToken>())).Throws<Exception>();
        processService.Setup(m => m.GetCurrentProcess()).Returns(process.Object);

        var observer = new DeserializeTransportMessageObserver(
            busOptions,
            serializer.Object,
            new Mock<IEnvironmentService>().Object,
            processService.Object);

        var pipeline = Pipeline.Get()
            .AddObserver(observer);

        pipeline
            .AddStage(".")
            .WithEvent<DeserializeTransportMessage>();

        var transportMessage = new TransportMessage();

        pipeline.State.SetReceivedMessage(new(Stream.Null, Guid.NewGuid()));
        pipeline.State.SetTransportMessage(transportMessage);
        pipeline.State.SetWorkTransport(workTransport.Object);
        pipeline.State.SetErrorTransport(new Mock<ITransport>().Object);

        await pipeline.ExecuteAsync(CancellationToken.None);

        serializer.Verify(m => m.DeserializeAsync(It.IsAny<Type>(), It.IsAny<Stream>(), It.IsAny<CancellationToken>()), Times.Exactly(2));

        process.Verify(m => m.Kill(), Times.Once);

        workTransport.VerifyNoOtherCalls();
        serializer.VerifyNoOtherCalls();
    }
}