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

        transportService.Setup(m => m.GetAsync(It.IsAny<Uri>(), CancellationToken.None)).ReturnsAsync(fakeTransport);

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
                WorkTransportUri = "fake://work",
                ErrorTransportUri = "fake://error",
                ThreadCount = 1
            };
        });

        var serviceBus = services.BuildServiceProvider().GetRequiredService<IServiceBus>();

        await using (await serviceBus.StartAsync())
        {
            var timeout = DateTime.Now.AddSeconds(5);

            while (fakeTransport.MessageCount < 2 && DateTime.Now < timeout)
            {
                Thread.Sleep(5);
            }
        }

        Assert.That(handlerInvoker.GetInvokeCount("SimpleCommand"), Is.EqualTo(1), "FakeHandlerInvoker was not invoked exactly once.");
        Assert.That(fakeTransport.MessageCount, Is.EqualTo(2), "FakeTransport was not invoked exactly twice.");
    }
}