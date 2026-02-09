using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using Shuttle.Core.Contract;

namespace Shuttle.Hopper.Tests;

[TestFixture]
public class MessageHandlerInvokerFixture
{
    public interface IMessageHandlerTracker
    {
        int HandledCount { get; }
        void Handled();
    }

    public class MessageHandlerTracker : IMessageHandlerTracker
    {
        public int HandledCount { get; private set; }

        public void Handled()
        {
            HandledCount++;
        }
    }

    public class Message
    {
        public int Count { get; set; }
        public string Name { get; set; } = string.Empty;
        public bool Replied { get; set; }
    }

    public class MessageHandler(IMessageHandlerTracker messageHandlerTracker) : IMessageHandler<Message>
    {
        private readonly IMessageHandlerTracker _messageHandlerTracker = Guard.AgainstNull(messageHandlerTracker);

        public async Task ProcessMessageAsync(IHandlerContext<Message> context, CancellationToken cancellationToken = default)
        {
            Console.WriteLine($@"[handled] : name = {context.Message.Name}");

            _messageHandlerTracker.Handled();

            if (context.Message.Replied)
            {
                Assert.That(!context.TransportMessage.MessageReceivedId.Equals(Guid.Empty), "Should have a `MessageReceivedId` value since the message is handled after being sent from a related message.");

                return;
            }

            await context.SendAsync(new Message
            {
                Replied = true,
                Name = $"replied-{context.Message.Count}"
            }, builder =>
            {
                builder.AsReply();
            }, cancellationToken);
        }
    }
    public class DirectMessageHandler(IBus bus, IMessageHandlerTracker messageHandlerTracker) : IDirectMessageHandler<Message>
    {
        private readonly IMessageHandlerTracker _messageHandlerTracker = Guard.AgainstNull(messageHandlerTracker);

        public async Task ProcessMessageAsync(Message message, CancellationToken cancellationToken = default)
        {
            Console.WriteLine($@"[handled] : name = {message.Name}");

            _messageHandlerTracker.Handled();

            if (message.Replied)
            {
                return;
            }

            await bus.SendAsync(new Message
            {
                Replied = true,
                Name = $"replied-{message.Count}"
            }, cancellationToken: cancellationToken);
        }
    }

    [Test]
    public async Task Should_be_able_to_invoke_handler_type_async()
    {
        const int count = 5;

        var services = new ServiceCollection();

        services.AddHopper(builder =>
        {
            builder.Options.Inbox.ThreadCount = 1;
            builder.Options.Inbox.WorkTransportUri = new("memory://configuration/inbox");
            builder.Options.Inbox.IdleDurations = [TimeSpan.FromMilliseconds(5)];
            builder.Options.MessageRoutes.Add(new()
            {
                Uri = "memory://configuration/inbox",
                Specifications =
                [
                    new()
                    {
                        Name = "StartsWith",
                        Value = "Shuttle"
                    }
                ]
            });
        });

        services.AddSingleton<ITransportFactory, MemoryTransportFactory>();
        services.AddSingleton<IMessageHandlerTracker, MessageHandlerTracker>();

        var serviceProvider = services.BuildServiceProvider();

        var messageHandlerTracker = serviceProvider.GetRequiredService<IMessageHandlerTracker>();

        DateTimeOffset timeout;

        await using (var bus = await serviceProvider.GetRequiredService<IBusControl>().StartAsync().ConfigureAwait(false))
        {
            for (var i = 0; i < count; i++)
            {
                await bus.SendAsync(new Message
                {
                    Count = i + 1,
                    Name = $"message - {i + 1}"
                });
            }

            timeout = DateTimeOffset.UtcNow.AddSeconds(5);

            while (messageHandlerTracker.HandledCount < count * 2 && DateTimeOffset.UtcNow < timeout)
            {
                Thread.Sleep(25);
            }
        }

        Assert.That(timeout > DateTimeOffset.UtcNow, "Timed out before all messages were handled.");
    }

    [Test]
    public async Task Should_be_able_to_invoke_context_handler_instance_async()
    {
        const int count = 5;

        var services = new ServiceCollection();

        var messageHandlerTracker = new MessageHandlerTracker();
        var contextHandler = new MessageHandler(messageHandlerTracker);

        services.AddHopper(builder =>
        {
            builder.Options.Inbox.ThreadCount = 1;
            builder.Options.Inbox.WorkTransportUri = new("memory://configuration/inbox");
            builder.Options.Inbox.IdleDurations = [TimeSpan.FromMilliseconds(5)];
            builder.Options.MessageRoutes.Add(new()
            {
                Uri = "memory://configuration/inbox",
                Specifications =
                [
                    new()
                    {
                        Name = "StartsWith",
                        Value = "Shuttle"
                    }
                ]
            });

            builder.Options.AddMessageHandlers = false;

            builder.AddMessageHandler(contextHandler);
        });

        services.AddSingleton<ITransportFactory, MemoryTransportFactory>();
        services.AddSingleton<IMessageHandlerTracker>(messageHandlerTracker);

        var serviceProvider = services.BuildServiceProvider();

        DateTimeOffset timeout;

        await using (var bus = await serviceProvider.GetRequiredService<IBusControl>().StartAsync().ConfigureAwait(false))
        {
            for (var i = 0; i < count; i++)
            {
                await bus.SendAsync(new Message
                {
                    Count = i + 1,
                    Name = $"message - {i + 1}"
                });
            }

            timeout = DateTimeOffset.UtcNow.AddSeconds(500);

            while (messageHandlerTracker.HandledCount < count * 2 && DateTimeOffset.UtcNow < timeout)
            {
                Thread.Sleep(25);
            }
        }

        Assert.That(timeout > DateTimeOffset.UtcNow, "Timed out before all messages were handled.");
    }

    [Test]
    public async Task Should_be_able_to_invoke_message_handler_instance_async()
    {
        const int count = 5;

        var services = new ServiceCollection()
            .AddSingleton<IMessageHandlerTracker, MessageHandlerTracker>()
            .AddSingleton<IDirectMessageHandler<Message>, DirectMessageHandler>();

        services.AddHopper(builder =>
        {
            builder.Options.Inbox.ThreadCount = 1;
            builder.Options.Inbox.WorkTransportUri = new("memory://configuration/inbox");
            builder.Options.Inbox.IdleDurations = [TimeSpan.FromMilliseconds(5)];
            builder.Options.MessageRoutes.Add(new()
            {
                Uri = "memory://configuration/inbox",
                Specifications =
                [
                    new()
                    {
                        Name = "StartsWith",
                        Value = "Shuttle"
                    }
                ]
            });

            builder.Options.AddMessageHandlers = false;
        });

        services.AddSingleton<ITransportFactory, MemoryTransportFactory>();

        var serviceProvider = services.BuildServiceProvider();

        var messageHandlerTracker = serviceProvider.GetRequiredService<IMessageHandlerTracker>();

        DateTimeOffset timeout;

        await using (var bus = await serviceProvider.GetRequiredService<IBusControl>().StartAsync().ConfigureAwait(false))
        {
            for (var i = 0; i < count; i++)
            {
                await bus.SendAsync(new Message
                {
                    Count = i + 1,
                    Name = $"message - {i + 1}"
                });
            }

            timeout = DateTimeOffset.UtcNow.AddSeconds(500);

            while (messageHandlerTracker.HandledCount < count * 2 && DateTimeOffset.UtcNow < timeout)
            {
                Thread.Sleep(25);
            }
        }

        Assert.That(timeout > DateTimeOffset.UtcNow, "Timed out before all messages were handled.");
    }
}