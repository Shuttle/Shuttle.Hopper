using System.Collections.Concurrent;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Moq;
using NUnit.Framework;
using Shuttle.Core.Pipelines;
using Shuttle.Core.Streams;

namespace Shuttle.Hopper.Tests.MessageHandling;

[TestFixture]
public class MessageHandlerInvokerFixture
{
    [Test]
    public async Task Should_be_able_to_invoke_message_handler_async()
    {
        var services = new ServiceCollection();

        services.AddSingleton(typeof(IContextHandler<>).MakeGenericType(typeof(WorkMessage)), typeof(WorkHandler));

        var serviceProvider = services.BuildServiceProvider();

        var invoker = new MessageHandlerInvoker(serviceProvider, new Mock<IMessageSender>().Object, new ContextHandlerDelegateRegistry(new Dictionary<Type, ContextHandlerDelegate>()), new MessageHandlerDelegateRegistry(new ConcurrentDictionary<Type, MessageHandlerDelegate>()));

        var transportMessage = new TransportMessage
        {
            Message = await Stream.Null.ToBytesAsync()
        };

        var pipelineContext = new PipelineContext<HandleMessage>(new Pipeline(PipelineDependencies.Empty()));

        pipelineContext.Pipeline.State.Add(StateKeys.Message, new WorkMessage());
        pipelineContext.Pipeline.State.Add(StateKeys.TransportMessage, transportMessage);

        Assert.That(await invoker.InvokeAsync(pipelineContext), Is.True);
    }

    [Test]
    public async Task Should_be_able_to_invoke_context_delegate_async()
    {
        var services = new ServiceCollection();

        var builder = new ServiceBusBuilder(services)
            .AddMessageHandler(async (IHandlerContext<WorkMessage> context) =>
            {
                Console.WriteLine($@"[work-message] : guid = {context.Message.Guid}");

                await Task.CompletedTask;
            });

        var serviceProvider = services.BuildServiceProvider();

        var invoker = new MessageHandlerInvoker(serviceProvider, new Mock<IMessageSender>().Object, new ContextHandlerDelegateRegistry(builder.GetContextHandlerDelegates()), new MessageHandlerDelegateRegistry(builder.GetMessageHandlerDelegates()));

        var transportMessage = new TransportMessage
        {
            Message = await Stream.Null.ToBytesAsync()
        };

        var pipelineContext = new PipelineContext<HandleMessage>(new Pipeline(PipelineDependencies.Empty()));

        pipelineContext.Pipeline.State.Add(StateKeys.Message, new WorkMessage());
        pipelineContext.Pipeline.State.Add(StateKeys.TransportMessage, transportMessage);

        Assert.That(await invoker.InvokeAsync(pipelineContext), Is.True);
    }

    [Test]
    public async Task Should_be_able_to_invoke_message_delegate_async()
    {
        var services = new ServiceCollection();

        var builder = new ServiceBusBuilder(services)
            .AddMessageHandler(async (WorkMessage message, CancellationToken cancellationToken) =>
            {
                Console.WriteLine($@"[work-message] : guid = {message.Guid}");

                await Task.Delay(TimeSpan.Zero, cancellationToken);
            });

        var serviceProvider = services.BuildServiceProvider();

        var invoker = new MessageHandlerInvoker(serviceProvider, new Mock<IMessageSender>().Object, new ContextHandlerDelegateRegistry(builder.GetContextHandlerDelegates()), new MessageHandlerDelegateRegistry(builder.GetMessageHandlerDelegates()));

        var transportMessage = new TransportMessage
        {
            Message = await Stream.Null.ToBytesAsync()
        };

        var pipelineContext = new PipelineContext<HandleMessage>(new Pipeline(PipelineDependencies.Empty()));

        pipelineContext.Pipeline.State.Add(StateKeys.Message, new WorkMessage());
        pipelineContext.Pipeline.State.Add(StateKeys.TransportMessage, transportMessage);

        Assert.That(await invoker.InvokeAsync(pipelineContext), Is.True);
    }
}