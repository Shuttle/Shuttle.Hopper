using Microsoft.Extensions.Options;
using Moq;
using NUnit.Framework;
using Shuttle.Core.Pipelines;
using Shuttle.Core.Serialization;

namespace Shuttle.Hopper.Tests;

[TestFixture]
public class HandleMessageObserverFixture
{
    [Test]
    public async Task Should_be_able_to_return_when_no_message_handling_is_required_async()
    {
        var messageHandlerInvoker = new Mock<IMessageHandlerInvoker>();
        var serializer = new Mock<ISerializer>();

        var observer = new HandleMessageObserver(Options.Create(new ServiceBusOptions()), messageHandlerInvoker.Object, serializer.Object);

        var pipeline = new Pipeline(PipelineDependencies.Empty())
            .AddObserver(observer);

        pipeline
            .AddStage(".")
            .WithEvent<HandleMessage>();

        pipeline.State.SetTransportMessage(new() { ExpiryDate = DateTime.Now.AddDays(-1) });

        await pipeline.ExecuteAsync();

        messageHandlerInvoker.VerifyNoOtherCalls();
        serializer.VerifyNoOtherCalls();
    }

    [Test]
    public async Task Should_be_able_to_execute_successful_invoke_async()
    {
        var messageHandlerInvoker = new Mock<IMessageHandlerInvoker>();
        var serializer = new Mock<ISerializer>();

        var observer = new HandleMessageObserver(Options.Create(new ServiceBusOptions()), messageHandlerInvoker.Object, serializer.Object);

        var pipeline = new Pipeline(PipelineDependencies.Empty())
            .AddObserver(observer);

        pipeline
            .AddStage(".")
            .WithEvent<HandleMessage>();

        var transportMessage = new TransportMessage();
        var message = new object();

        pipeline.State.SetTransportMessage(transportMessage);
        pipeline.State.SetMessage(message);

        messageHandlerInvoker.Setup(m => m.InvokeAsync(It.IsAny<IPipelineContext<HandleMessage>>(), It.IsAny<CancellationToken>())).Returns(ValueTask.FromResult(true));

        await pipeline.ExecuteAsync();

        messageHandlerInvoker.Verify(m => m.InvokeAsync(It.IsAny<IPipelineContext<HandleMessage>>(), It.IsAny<CancellationToken>()), Times.Once);

        Assert.That(pipeline.State.GetMessageHandlerInvoked(), Is.True);

        messageHandlerInvoker.VerifyNoOtherCalls();
        serializer.VerifyNoOtherCalls();
    }

    [Test]
    public async Task Should_be_able_to_execute_missing_handler_async()
    {
        var workTransport = new Mock<ITransport>();
        var errorTransport = new Mock<ITransport>();
        var messageHandlerInvoker = new Mock<IMessageHandlerInvoker>();
        var serializer = new Mock<ISerializer>();
        var messageNotHandledCount = 0;
        var handlerExceptionCount = 0;

        var serviceBusOptions = new ServiceBusOptions();

        serviceBusOptions.MessageNotHandled += async (_, _) =>
        {
            messageNotHandledCount++;

            await Task.CompletedTask;
        };

        serviceBusOptions.HandlerException += async (_, _) =>
        {
            handlerExceptionCount++;

            await Task.CompletedTask;
        };


        var observer = new HandleMessageObserver(Options.Create(serviceBusOptions), messageHandlerInvoker.Object, serializer.Object);

        errorTransport.Setup(m => m.Uri).Returns(new TransportUri("transport://configuration/name"));

        var pipeline = new Pipeline(PipelineDependencies.Empty())
            .AddObserver(observer);

        pipeline
            .AddStage(".")
            .WithEvent<HandleMessage>();

        var transportMessage = new TransportMessage();
        var message = new object();

        pipeline.State.SetTransportMessage(transportMessage);
        pipeline.State.SetMessage(message);
        pipeline.State.SetWorkTransport(workTransport.Object);
        pipeline.State.SetErrorTransport(errorTransport.Object);

        messageHandlerInvoker.Setup(m => m.InvokeAsync(It.IsAny<IPipelineContext<HandleMessage>>(), It.IsAny<CancellationToken>())).Returns(ValueTask.FromResult(false));

        await pipeline.ExecuteAsync();

        messageHandlerInvoker.Verify(m => m.InvokeAsync(It.IsAny<IPipelineContext<HandleMessage>>(), It.IsAny<CancellationToken>()));

        errorTransport.Verify(m => m.SendAsync(transportMessage, It.IsAny<Stream>(), It.IsAny<CancellationToken>()), Times.Once);
        serializer.Verify(m => m.SerializeAsync(transportMessage, It.IsAny<CancellationToken>()));

        Assert.That(pipeline.State.GetMessageHandlerInvoked(), Is.False);
        Assert.That(messageNotHandledCount, Is.EqualTo(1));
        Assert.That(handlerExceptionCount, Is.Zero);

        messageHandlerInvoker.VerifyNoOtherCalls();
        serializer.VerifyNoOtherCalls();
    }

    [Test]
    public async Task Should_be_able_to_remove_messages_not_handled_async()
    {
        var workTransport = new Mock<ITransport>();
        var messageHandlerInvoker = new Mock<IMessageHandlerInvoker>();
        var serializer = new Mock<ISerializer>();
        var messageNotHandledCount = 0;
        var handlerExceptionCount = 0;

        var serviceBusOptions = new ServiceBusOptions
        {
            RemoveMessagesNotHandled = true
        };

        serviceBusOptions.MessageNotHandled += async (_, _) =>
        {
            messageNotHandledCount++;

            await Task.CompletedTask;
        };

        serviceBusOptions.HandlerException += async (_, _) =>
        {
            handlerExceptionCount++;

            await Task.CompletedTask;
        };

        var observer = new HandleMessageObserver(Options.Create(serviceBusOptions), messageHandlerInvoker.Object, serializer.Object);

        var pipeline = new Pipeline(PipelineDependencies.Empty())
            .AddObserver(observer);

        pipeline
            .AddStage(".")
            .WithEvent<HandleMessage>();

        var transportMessage = new TransportMessage();
        var message = new object();

        pipeline.State.SetWorkTransport(workTransport.Object);
        pipeline.State.SetTransportMessage(transportMessage);
        pipeline.State.SetMessage(message);

        messageHandlerInvoker.Setup(m => m.InvokeAsync(It.IsAny<IPipelineContext<HandleMessage>>(), It.IsAny<CancellationToken>())).Returns(ValueTask.FromResult(false));

        await pipeline.ExecuteAsync();

        messageHandlerInvoker.Verify(m => m.InvokeAsync(It.IsAny<IPipelineContext<HandleMessage>>(), It.IsAny<CancellationToken>()), Times.Once);

        Assert.That(pipeline.State.GetMessageHandlerInvoked(), Is.False);
        Assert.That(messageNotHandledCount, Is.EqualTo(1));
        Assert.That(handlerExceptionCount, Is.Zero);

        messageHandlerInvoker.VerifyNoOtherCalls();
        serializer.VerifyNoOtherCalls();
    }

    [Test]
    public async Task Should_fail_on_missing_error_transport_async()
    {
        var workTransport = new Mock<ITransport>();
        var messageHandlerInvoker = new Mock<IMessageHandlerInvoker>();
        var serializer = new Mock<ISerializer>();
        var messageNotHandledCount = 0;
        var handlerExceptionCount = 0;

        var serviceBusOptions = new ServiceBusOptions();

        serviceBusOptions.MessageNotHandled += async (_, _) =>
        {
            messageNotHandledCount++;

            await Task.CompletedTask;
        };

        serviceBusOptions.HandlerException += async (_, _) =>
        {
            handlerExceptionCount++;

            await Task.CompletedTask;
        };

        var observer = new HandleMessageObserver(Options.Create(serviceBusOptions), messageHandlerInvoker.Object, serializer.Object);

        var pipeline = new Pipeline(PipelineDependencies.Empty())
            .AddObserver(observer);

        pipeline
            .AddStage(".")
            .WithEvent<HandleMessage>();

        var transportMessage = new TransportMessage();
        var message = new object();

        pipeline.State.SetWorkTransport(workTransport.Object);
        pipeline.State.SetTransportMessage(transportMessage);
        pipeline.State.SetMessage(message);

        messageHandlerInvoker.Setup(m => m.InvokeAsync(It.IsAny<IPipelineContext<HandleMessage>>(), It.IsAny<CancellationToken>())).Returns(ValueTask.FromResult(false));

        Assert.ThrowsAsync<Core.Pipelines.PipelineException>(() => pipeline.ExecuteAsync());

        messageHandlerInvoker.Verify(m => m.InvokeAsync(It.IsAny<IPipelineContext<HandleMessage>>(), It.IsAny<CancellationToken>()), Times.Once);

        Assert.That(pipeline.State.GetMessageHandlerInvoked(), Is.False);
        Assert.That(messageNotHandledCount, Is.EqualTo(1));
        Assert.That(handlerExceptionCount, Is.EqualTo(1));

        messageHandlerInvoker.VerifyNoOtherCalls();
        serializer.VerifyNoOtherCalls();

        await Task.CompletedTask;
    }
}