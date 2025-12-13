using Moq;
using NUnit.Framework;
using Shuttle.Core.Pipelines;

namespace Shuttle.Hopper.Tests;

[TestFixture]
public class FindMessageRouteObserverFixture
{
    [Test]
    public async Task Should_be_able_to_skip_when_there_is_already_a_recipient_async()
    {
        var messageRouteProvider = new Mock<IMessageRouteProvider>();

        var observer = new FindMessageRouteObserver(messageRouteProvider.Object);

        var pipeline = new Pipeline(PipelineDependencies.Empty())
            .AddObserver(observer);

        pipeline
            .AddStage(".")
            .WithEvent<FindMessageRoute>();

        pipeline.State.SetTransportMessage(new() { RecipientInboxWorkTransportUri = "recipient-uri" });

        await pipeline.ExecuteAsync();

        messageRouteProvider.VerifyNoOtherCalls();
    }

    [Test]
    public void Should_throw_exception_when_no_route_found_async()
    {
        var messageRouteProvider = new Mock<IMessageRouteProvider>();
        const string messageType = "message-type";

        messageRouteProvider.Setup(m => m.GetRouteUrisAsync(messageType, It.IsAny<CancellationToken>())).Returns(Task.FromResult(Enumerable.Empty<string>()));

        var observer = new FindMessageRouteObserver(messageRouteProvider.Object);

        var pipeline = new Pipeline(PipelineDependencies.Empty())
            .AddObserver(observer);

        pipeline
            .AddStage(".")
            .WithEvent<FindMessageRoute>();

        var transportMessage = new TransportMessage { MessageType = messageType };

        pipeline.State.SetTransportMessage(transportMessage);

        var exception = Assert.ThrowsAsync<Core.Pipelines.PipelineException>(() => pipeline.ExecuteAsync())!;

        messageRouteProvider.Verify(m => m.GetRouteUrisAsync(messageType, It.IsAny<CancellationToken>()), Times.Once);

        Assert.That(exception, Is.Not.Null);
        Assert.That(exception.InnerException?.Message, Contains.Substring("No route could be found"));

        messageRouteProvider.VerifyNoOtherCalls();
    }

    [Test]
    public void Should_throw_exception_when_multiple_routes_found_async()
    {
        var messageRouteProvider = new Mock<IMessageRouteProvider>();
        const string messageType = "message-type";
        var routes = new List<string> { "route-a", "route-b" };

        messageRouteProvider.Setup(m => m.GetRouteUrisAsync(messageType, It.IsAny<CancellationToken>())).Returns(Task.FromResult(routes.AsEnumerable()));

        var observer = new FindMessageRouteObserver(messageRouteProvider.Object);

        var pipeline = new Pipeline(PipelineDependencies.Empty())
            .AddObserver(observer);

        pipeline
            .AddStage(".")
            .WithEvent<FindMessageRoute>();

        var transportMessage = new TransportMessage { MessageType = messageType };

        pipeline.State.SetTransportMessage(transportMessage);

        var exception = Assert.ThrowsAsync<Core.Pipelines.PipelineException>(() => pipeline.ExecuteAsync());

        messageRouteProvider.Verify(m => m.GetRouteUrisAsync(messageType, It.IsAny<CancellationToken>()), Times.Once);

        Assert.That(exception, Is.Not.Null);
        Assert.That(exception!.InnerException?.Message, Contains.Substring("has been routed to more than one endpoint"));

        messageRouteProvider.VerifyNoOtherCalls();
    }

    [Test]
    public async Task Should_be_able_to_route_to_single_endpoint_async()
    {
        var messageRouteProvider = new Mock<IMessageRouteProvider>();
        const string messageType = "message-type";
        var routes = new List<string> { "route-a" };

        messageRouteProvider.Setup(m => m.GetRouteUrisAsync(messageType, It.IsAny<CancellationToken>())).Returns(Task.FromResult(routes.AsEnumerable()));

        var observer = new FindMessageRouteObserver(messageRouteProvider.Object);

        var pipeline = new Pipeline(PipelineDependencies.Empty())
            .AddObserver(observer);

        pipeline
            .AddStage(".")
            .WithEvent<FindMessageRoute>();

        var transportMessage = new TransportMessage { MessageType = messageType };

        pipeline.State.SetTransportMessage(transportMessage);

        await pipeline.ExecuteAsync();

        messageRouteProvider.Verify(m => m.GetRouteUrisAsync(messageType, It.IsAny<CancellationToken>()), Times.Once);

        Assert.That(transportMessage.RecipientInboxWorkTransportUri, Is.EqualTo("route-a"));

        messageRouteProvider.VerifyNoOtherCalls();
    }
}