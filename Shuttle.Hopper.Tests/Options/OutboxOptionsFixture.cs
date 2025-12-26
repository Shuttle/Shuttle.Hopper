using NUnit.Framework;

namespace Shuttle.Hopper.Tests;

[TestFixture]
public class OutboxOptionsFixture : OptionsFixture
{
    [Test]
    public void Should_be_able_to_load_a_full_configuration()
    {
        var options = GetOptions();

        Assert.That(options, Is.Not.Null);

        Assert.That(options.Outbox!.WorkTransportUri, Is.EqualTo("transport://./outbox-work"));
        Assert.That(options.Outbox.ErrorTransportUri, Is.EqualTo("transport://./outbox-error"));

        Assert.That(options.Outbox.MaximumFailureCount, Is.EqualTo(25));

        Assert.That(options.Outbox.IdleDurations[0], Is.EqualTo(TimeSpan.FromMilliseconds(250)));
        Assert.That(options.Outbox.IdleDurations[1], Is.EqualTo(TimeSpan.FromSeconds(10)));
        Assert.That(options.Outbox.IdleDurations[2], Is.EqualTo(TimeSpan.FromSeconds(30)));

        Assert.That(options.Outbox.IgnoreOnFailureDurations[0], Is.EqualTo(TimeSpan.FromMinutes(30)));
        Assert.That(options.Outbox.IgnoreOnFailureDurations[1], Is.EqualTo(TimeSpan.FromHours(1)));
    }
}