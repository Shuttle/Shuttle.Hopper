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
        var hopperOptions = new HopperOptions();
        var identityProvider = new Mock<IIdentityProvider>();
        var transportMessage = new TransportMessage
        {
            SenderInboxWorkTransportUri = "null-transport://./work-transport"
        };
        var builder = new TransportMessageBuilder(transportMessage);

        var transportService = new Mock<ITransportService>();

        transportService.Setup(m => m.GetAsync(It.IsAny<Uri>(), It.IsAny<CancellationToken>())).ReturnsAsync((Uri uri, CancellationToken _) => new NullTransport(hopperOptions, uri));

        identityProvider.Setup(m => m.Get()).Returns(new GenericIdentity(Environment.UserDomainName + "\\" + Environment.UserName, "Anonymous"));

        Assert.That(transportMessage.SenderInboxWorkTransportUri, Is.EqualTo("null-transport://./work-transport"));

        builder.WithSender("null-transport://./another-transport");

        Assert.That(transportMessage.SenderInboxWorkTransportUri, Is.EqualTo("null-transport://./another-transport"));
    }

    [Test]
    public void Should_not_allow_an_inbox_name_together_with_another_recipient()
    {
        Assert.That(new TransportMessageBuilder(new()).ToInbox("priority").InboxName, Is.EqualTo("priority"));

        Assert.Throws<InvalidOperationException>(() => new TransportMessageBuilder(new()).ToInbox("priority").ToSelf());
        Assert.Throws<InvalidOperationException>(() => new TransportMessageBuilder(new()).ToSelf().ToInbox("priority"));
        Assert.Throws<InvalidOperationException>(() => new TransportMessageBuilder(new()).WithRecipient("null-transport://./work-transport").ToInbox("priority"));
        Assert.Throws<InvalidOperationException>(() => new TransportMessageBuilder(new()).ToInbox("priority").AsReply());
    }
}