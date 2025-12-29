using NUnit.Framework;

namespace Shuttle.Hopper.Tests;

[TestFixture]
public class TransportMessageFixture
{
    [Test]
    public void Should_be_able_to_determine_if_message_has_an_expiry_date()
    {
        Assert.That(new TransportMessage().HasExpiryDate(), Is.False);

        Assert.That(new TransportMessage { ExpiryDateTime = DateTimeOffset.MaxValue }.HasExpiryDate(), Is.False);

        Assert.That(new TransportMessage { ExpiryDateTime = DateTimeOffset.UtcNow.AddSeconds(30) }.HasExpiryDate(), Is.True);
    }

    [Test]
    public void Should_be_able_to_determine_if_message_has_expired()
    {
        Assert.That(new TransportMessage().HasExpired(), Is.False);

        Assert.That(new TransportMessage { ExpiryDateTime = DateTimeOffset.MaxValue }.HasExpired(), Is.False);

        Assert.That(new TransportMessage { ExpiryDateTime = DateTimeOffset.UtcNow.AddSeconds(-30) }.HasExpired(), Is.True);
    }

    [Test]
    public void Should_be_able_to_determine_if_message_should_be_ignored()
    {
        var transportMessage = new TransportMessage
        {
            IgnoreTillDateTime = DateTimeOffset.UtcNow.AddMinutes(1)
        };

        Assert.That(transportMessage.IsIgnoring(), Is.True);

        transportMessage.IgnoreTillDateTime = DateTimeOffset.UtcNow.AddMilliseconds(-1);

        Assert.That(transportMessage.IsIgnoring(), Is.False);
    }

    [Test]
    public void Should_be_able_to_register_failures_and_have_IgnoreTillDate_set()
    {
        var message = new TransportMessage();

        var before = DateTimeOffset.UtcNow;

        message.RegisterFailure("failure");

        Assert.That(before <= message.IgnoreTillDateTime, Is.True);

        message = new();

        var durationToIgnoreOnFailure =
            new[]
            {
                TimeSpan.FromMinutes(3),
                TimeSpan.FromMinutes(30),
                TimeSpan.FromHours(2)
            };

        Assert.That(DateTimeOffset.UtcNow.AddMinutes(3) <= message.IgnoreTillDateTime, Is.False);

        message.RegisterFailure("failure", durationToIgnoreOnFailure[0]);

        var ignoreTillDate = DateTimeOffset.UtcNow.AddMinutes(3);

        Assert.That(ignoreTillDate.AddMilliseconds(-100) < message.IgnoreTillDateTime && ignoreTillDate.AddMilliseconds(100) > message.IgnoreTillDateTime, Is.True);
        Assert.That(DateTimeOffset.UtcNow.AddMinutes(30) < message.IgnoreTillDateTime, Is.False);

        message.RegisterFailure("failure", durationToIgnoreOnFailure[1]);

        ignoreTillDate = DateTimeOffset.UtcNow.AddMinutes(30);

        Assert.That(ignoreTillDate.AddMilliseconds(-100) < message.IgnoreTillDateTime && ignoreTillDate.AddMilliseconds(100) > message.IgnoreTillDateTime, Is.True);
        Assert.That(DateTimeOffset.UtcNow.AddHours(2) < message.IgnoreTillDateTime, Is.False);

        message.RegisterFailure("failure", durationToIgnoreOnFailure[2]);

        ignoreTillDate = DateTimeOffset.UtcNow.AddHours(2);

        Assert.That(ignoreTillDate.AddMilliseconds(-100) < message.IgnoreTillDateTime && ignoreTillDate.AddMilliseconds(100) > message.IgnoreTillDateTime, Is.True);
    }
}