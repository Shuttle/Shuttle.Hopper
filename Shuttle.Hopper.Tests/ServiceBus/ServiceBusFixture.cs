using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using NUnit.Framework;
using Shuttle.Core.TransactionScope;

namespace Shuttle.Hopper.Tests;

[TestFixture]
public class ServiceBusFixture
{
    [Test]
    public async Task Should_be_able_to_handle_expired_message_async()
    {
        var handlerInvoker = new FakeMessageHandlerInvoker();
        var fakeTransport = new FakeTransport(new(), 2);

        var transportService = new Mock<ITransportService>();

        transportService.Setup(m => m.GetAsync(It.IsAny<Uri>(), It.IsAny<CancellationToken>())).ReturnsAsync(fakeTransport);

        var services = new ServiceCollection();

        services.AddSingleton<IConfiguration>(new ConfigurationBuilder().Build());
        services.AddTransactionScope(builder =>
        {
            builder.Options.Enabled = false;
        });
        services.AddSingleton(transportService.Object);
        services.AddSingleton<IMessageHandlerInvoker>(handlerInvoker);
        services.AddServiceBus(builder =>
        {
            builder.Options.Inbox = new()
            {
                WorkTransportUri = new ("fake://work"),
                ErrorTransportUri = new("fake://error"),
                ThreadCount = 1
            };
        });

        var serviceBus = services.BuildServiceProvider().GetRequiredService<IServiceBus>();

        await using (await serviceBus.StartAsync())
        {
            var timeout = DateTimeOffset.UtcNow.AddSeconds(5);

            while (fakeTransport.MessageCount < 2 && DateTimeOffset.UtcNow < timeout)
            {
                Thread.Sleep(5);
            }
        }

        Assert.That(handlerInvoker.GetInvokeCount("SimpleCommand"), Is.EqualTo(1), "FakeHandlerInvoker was not invoked exactly once.");
        Assert.That(fakeTransport.MessageCount, Is.EqualTo(2), "FakeTransport was not invoked exactly twice.");
    }
}