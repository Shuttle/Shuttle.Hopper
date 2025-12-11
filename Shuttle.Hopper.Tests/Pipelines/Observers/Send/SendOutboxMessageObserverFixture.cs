using Moq;
using NUnit.Framework;
using Shuttle.Core.Pipelines;

namespace Shuttle.Hopper.Tests;

[TestFixture]
public class SendOutboxMessageObserverFixture
{
    [Test]
    public void Should_throw_exception_on_invariant_failure_async()
    {
        var transportService = new Mock<ITransportService>();

        var observer = new SendOutboxMessageObserver(transportService.Object);

        var pipeline = new Pipeline(PipelineDependencies.Empty())
            .AddObserver(observer);

        pipeline
            .AddStage(".")
            .WithEvent<DispatchTransportMessage>();

        var exception = Assert.ThrowsAsync<Core.Pipelines.PipelineException>(async () => await pipeline.ExecuteAsync())!;

        Assert.That(exception, Is.Not.Null);
        Assert.That(exception.InnerException?.Message, Contains.Substring(StateKeys.TransportMessage));

        pipeline.State.SetTransportMessage(new());

        exception = Assert.ThrowsAsync<Core.Pipelines.PipelineException>(() => pipeline.ExecuteAsync())!;

        Assert.That(exception, Is.Not.Null);
        Assert.That(exception.InnerException?.Message, Contains.Substring(StateKeys.ReceivedMessage));

        pipeline.State.SetReceivedMessage(new(Stream.Null, Guid.NewGuid()));

        exception = Assert.ThrowsAsync<Core.Pipelines.PipelineException>(() => pipeline.ExecuteAsync())!;

        Assert.That(exception, Is.Not.Null);
        Assert.That(exception.InnerException?.Message, Contains.Substring(nameof(TransportMessage.RecipientInboxWorkTransportUri)));

        transportService.VerifyNoOtherCalls();
    }

    [Test]
    public async Task Should_be_able_to_enqueue_into_recipient_transport_async()
    {
        var transportService = new Mock<ITransportService>();
        var recipientTransport = new Mock<ITransport>();

        var observer = new SendOutboxMessageObserver(transportService.Object);

        var pipeline = new Pipeline(PipelineDependencies.Empty())
            .AddObserver(observer);

        pipeline
            .AddStage(".")
            .WithEvent<DispatchTransportMessage>();

        var transportMessage = new TransportMessage { RecipientInboxWorkTransportUri = "transport://host/somewhere" };

        pipeline.State.SetTransportMessage(transportMessage);
        pipeline.State.SetReceivedMessage(new(Stream.Null, Guid.NewGuid()));

        transportService.Setup(m => m.GetAsync(It.IsAny<Uri>(), CancellationToken.None)).ReturnsAsync(recipientTransport.Object);

        await pipeline.ExecuteAsync();

        recipientTransport.Verify(m => m.SendAsync(transportMessage, It.IsAny<Stream>(), CancellationToken.None));

        transportService.Verify(m => m.GetAsync(It.IsAny<Uri>(), CancellationToken.None), Times.Once);

        transportService.VerifyNoOtherCalls();
        recipientTransport.VerifyNoOtherCalls();
    }
}