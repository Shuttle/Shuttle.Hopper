using System.Security.Principal;
using Moq;
using NUnit.Framework;

namespace Shuttle.Hopper.Tests;

[TestFixture]
public class TransportMessageBuilderFixture
{
    [Test]
    public void Should_be_able_to_set_sender()
    {
        var serviceBusOptions = new ServiceBusOptions();
        var identityProvider = new Mock<IIdentityProvider>();
        var transportMessage = new TransportMessage
        {
            SenderInboxWorkTransportUri = "null-transport://./work-transport"
        };
        var builder = new TransportMessageBuilder(transportMessage);

        var transportService = new Mock<ITransportService>();

        transportService.Setup(m => m.GetAsync(It.IsAny<Uri>(), CancellationToken.None)).ReturnsAsync((Uri uri, CancellationToken _) => new NullTransport(serviceBusOptions, uri));

        identityProvider.Setup(m => m.Get()).Returns(new GenericIdentity(Environment.UserDomainName + "\\" + Environment.UserName, "Anonymous"));

        Assert.That(transportMessage.SenderInboxWorkTransportUri, Is.EqualTo("null-transport://./work-transport"));

        builder.WithSender("null-transport://./another-transport");

        Assert.That(transportMessage.SenderInboxWorkTransportUri, Is.EqualTo("null-transport://./another-transport"));
    }
}