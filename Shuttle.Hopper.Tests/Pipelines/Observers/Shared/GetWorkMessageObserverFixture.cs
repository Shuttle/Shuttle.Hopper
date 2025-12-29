using Moq;
using NUnit.Framework;
using Shuttle.Core.Pipelines;

namespace Shuttle.Hopper.Tests;

[TestFixture]
public class GetWorkMessageObserverFixture
{
    [Test]
    public void Should_throw_exception_when_required_state_is_missing_async()
    {
        var observer = new ReceiveWorkMessageObserver();

        var pipeline = new Pipeline(PipelineDependencies.Empty())
            .AddObserver(observer);

        pipeline
            .AddStage(".")
            .WithEvent<ReceiveMessage>();

        var exception = Assert.ThrowsAsync<Core.Pipelines.PipelineException>(() => pipeline.ExecuteAsync())!;

        Assert.That(exception, Is.Not.Null);
        Assert.That(exception.InnerException?.Message, Contains.Substring(StateKeys.WorkTransport));
    }

    [Test]
    public async Task Should_be_able_to_abort_pipeline_when_no_message_is_available_async()
    {
        var workTransport = new Mock<ITransport>();
        var observer = new ReceiveWorkMessageObserver();

        var pipeline = new Pipeline(PipelineDependencies.Empty())
            .AddObserver(observer);

        pipeline
            .AddStage(".")
            .WithEvent<ReceiveMessage>();

        workTransport.Setup(m => m.ReceiveAsync(CancellationToken.None)).Returns(Task.FromResult(null as ReceivedMessage));

        pipeline.State.SetWorkTransport(workTransport.Object);

        await pipeline.ExecuteAsync();

        workTransport.Verify(m => m.ReceiveAsync(CancellationToken.None), Times.Once);

        Assert.That(pipeline.Aborted, Is.True);
        Assert.That(pipeline.State.GetWorkPerformed(), Is.False);

        workTransport.VerifyNoOtherCalls();
    }

    [Test]
    public async Task Should_be_able_to_return_received_message_async()
    {
        var workTransport = new Mock<ITransport>();
        var observer = new ReceiveWorkMessageObserver();

        var pipeline = new Pipeline(PipelineDependencies.Empty())
            .AddObserver(observer);

        pipeline
            .AddStage(".")
            .WithEvent<ReceiveMessage>();

        var receivedMessage = new ReceivedMessage(Stream.Null, Guid.NewGuid());

        workTransport.Setup(m => m.ReceiveAsync(CancellationToken.None)).Returns(Task.FromResult(receivedMessage)!);

        pipeline.State.SetWorkTransport(workTransport.Object);

        await pipeline.ExecuteAsync();

        workTransport.Verify(m => m.ReceiveAsync(CancellationToken.None), Times.Once);

        Assert.That(pipeline.Aborted, Is.False);
        Assert.That(pipeline.State.GetWorkPerformed(), Is.True);
        Assert.That(pipeline.State.GetReceivedMessage(), Is.SameAs(receivedMessage));

        workTransport.VerifyNoOtherCalls();
    }
}