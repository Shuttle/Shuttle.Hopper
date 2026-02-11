using System.Collections.Concurrent;
using Microsoft.Extensions.DependencyInjection;
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
        var serviceProvider = new ServiceCollection()
            .AddSingleton(typeof(IDirectMessageHandler<>).MakeGenericType(typeof(WorkMessage)), typeof(WorkHandler))
            .AddScoped<IMessageContext, MessageContext>()
            .AddScoped<IMessageSenderContext, MessageSenderContext>()
            .AddScoped<IMessageSender, MessageSender>()
            .AddSingleton<ISubscriptionService>(_ => new Mock<ISubscriptionService>().Object)
            .AddSingleton(new Mock<IPipelineFactory>().Object)
            .BuildServiceProvider();

        var invoker = new MessageHandlerInvoker(new MessageHandlerDelegateRegistry(new Dictionary<Type, MessageHandlerDelegate>()), new DirectMessageHandlerDelegateRegistry(new ConcurrentDictionary<Type, DirectMessageHandlerDelegate>()));

        var transportMessage = new TransportMessage
        {
            Message = await Stream.Null.ToBytesAsync()
        };

        var pipelineContext = new PipelineContext<HandleMessage>(Pipeline.Get(serviceProvider));

        pipelineContext.Pipeline.State.Add(StateKeys.Message, new WorkMessage());
        pipelineContext.Pipeline.State.Add(StateKeys.TransportMessage, transportMessage);

        Assert.That(await invoker.InvokeAsync(pipelineContext), Is.True);
    }

    [Test]
    public async Task Should_be_able_to_invoke_context_delegate_async()
    {
        var services = new ServiceCollection();

        var builder = new HopperBuilder(services)
            .AddMessageHandler(async (WorkMessage message) =>
            {
                Console.WriteLine($@"[work-message] : guid = {message.Guid}");

                await Task.CompletedTask;
            });

        services
            .AddScoped<IMessageContext, MessageContext>()
            .AddScoped<IMessageSenderContext, MessageSenderContext>()
            .AddScoped<IMessageSender, MessageSender>()
            .AddSingleton<ISubscriptionService>(_ => new Mock<ISubscriptionService>().Object)
            .AddSingleton(new Mock<IPipelineFactory>().Object);

        var serviceProvider = services.BuildServiceProvider();

        var invoker = new MessageHandlerInvoker(new MessageHandlerDelegateRegistry(new Dictionary<Type, MessageHandlerDelegate>()), new DirectMessageHandlerDelegateRegistry(builder.GetDirectMessageHandlerDelegates()));

        var transportMessage = new TransportMessage
        {
            Message = await Stream.Null.ToBytesAsync()
        };

        var pipelineContext = new PipelineContext<HandleMessage>(Pipeline.Get(serviceProvider));

        pipelineContext.Pipeline.State.Add(StateKeys.Message, new WorkMessage());
        pipelineContext.Pipeline.State.Add(StateKeys.TransportMessage, transportMessage);

        Assert.That(await invoker.InvokeAsync(pipelineContext), Is.True);
    }

    [Test]
    public async Task Should_be_able_to_invoke_message_delegate_async()
    {
        var services = new ServiceCollection();

        var builder = new HopperBuilder(services)
            .AddMessageHandler(async (WorkMessage message, CancellationToken cancellationToken) =>
            {
                Console.WriteLine($@"[work-message] : guid = {message.Guid}");

                await Task.Delay(TimeSpan.Zero, cancellationToken);
            });

        services
            .AddScoped<IMessageContext, MessageContext>()
            .AddScoped<IMessageSenderContext, MessageSenderContext>()
            .AddScoped<IMessageSender, MessageSender>()
            .AddSingleton<ISubscriptionService>(_ => new Mock<ISubscriptionService>().Object)
            .AddSingleton(new Mock<IPipelineFactory>().Object);

        var serviceProvider = services.BuildServiceProvider();

        var invoker = new MessageHandlerInvoker(new MessageHandlerDelegateRegistry(new Dictionary<Type, MessageHandlerDelegate>()), new DirectMessageHandlerDelegateRegistry(builder.GetDirectMessageHandlerDelegates()));

        var transportMessage = new TransportMessage
        {
            Message = await Stream.Null.ToBytesAsync()
        };

        var pipelineContext = new PipelineContext<HandleMessage>(Pipeline.Get(serviceProvider));

        pipelineContext.Pipeline.State.Add(StateKeys.Message, new WorkMessage());
        pipelineContext.Pipeline.State.Add(StateKeys.TransportMessage, transportMessage);

        Assert.That(await invoker.InvokeAsync(pipelineContext), Is.True);
    }
}