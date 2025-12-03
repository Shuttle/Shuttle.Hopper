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
        var serviceBusOptions = Options.Create(new ServiceBusOptions
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
        serializer.Setup(m => m.DeserializeAsync(It.IsAny<Type>(), It.IsAny<Stream>(), CancellationToken.None)).Throws<Exception>();
        processService.Setup(m => m.GetCurrentProcess()).Returns(process.Object);

        var observer = new DeserializeTransportMessageObserver(
            serviceBusOptions,
            serializer.Object,
            new Mock<IEnvironmentService>().Object,
            processService.Object);

        var pipeline = new Pipeline(Options.Create(new PipelineOptions()), new Mock<IServiceProvider>().Object)
            .AddObserver(observer);

        pipeline
            .AddStage(".")
            .WithEvent<OnDeserializeTransportMessage>();

        var transportMessage = new TransportMessage();

        pipeline.State.SetReceivedMessage(new(Stream.Null, Guid.NewGuid()));
        pipeline.State.SetTransportMessage(transportMessage);
        pipeline.State.SetWorkTransport(workTransport.Object);
        pipeline.State.SetErrorTransport(errorTransport.Object);

        await pipeline.ExecuteAsync();

        process.Verify(m => m.Kill(), Times.Never);

        workTransport.Verify(m => m.AcknowledgeAsync(It.IsAny<object>(), CancellationToken.None), Times.Once);
    }

    [Test]
    public async Task Should_be_able_to_kill_process_when_corrupt_message_is_received_async()
    {
        var serviceBusOptions = Options.Create(new ServiceBusOptions
        {
            RemoveCorruptMessages = false
        });

        var workTransport = new Mock<ITransport>();
        var serializer = new Mock<ISerializer>();
        var processService = new Mock<IProcessService>();
        var process = new Mock<IProcess>();

        workTransport.Setup(m => m.Uri).Returns(new TransportUri("transport://configuration/work-transport"));
        serializer.Setup(m => m.DeserializeAsync(It.IsAny<Type>(), It.IsAny<Stream>(), CancellationToken.None)).Throws<Exception>();
        processService.Setup(m => m.GetCurrentProcess()).Returns(process.Object);

        var observer = new DeserializeTransportMessageObserver(
            serviceBusOptions,
            serializer.Object,
            new Mock<IEnvironmentService>().Object,
            processService.Object);

        var pipeline = new Pipeline(Options.Create(new PipelineOptions()), new Mock<IServiceProvider>().Object)
            .AddObserver(observer);

        pipeline
            .AddStage(".")
            .WithEvent<OnDeserializeTransportMessage>();

        var transportMessage = new TransportMessage();

        pipeline.State.SetReceivedMessage(new(Stream.Null, Guid.NewGuid()));
        pipeline.State.SetTransportMessage(transportMessage);
        pipeline.State.SetWorkTransport(workTransport.Object);
        pipeline.State.SetErrorTransport(new Mock<ITransport>().Object);

        await pipeline.ExecuteAsync(CancellationToken.None);

        serializer.Verify(m => m.DeserializeAsync(typeof(TransportMessage), It.IsAny<Stream>(), CancellationToken.None), Times.Once);

        process.Verify(m => m.Kill(), Times.Once);

        workTransport.VerifyNoOtherCalls();
        serializer.VerifyNoOtherCalls();
    }
}