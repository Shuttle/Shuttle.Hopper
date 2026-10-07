using NUnit.Framework;

namespace Shuttle.Hopper.Tests;

[TestFixture]
public class AdditionalInboxesOptionsFixture : OptionsFixture
{
    [Test]
    public void Should_be_able_to_load_additional_inboxes()
    {
        var options = GetOptions();

        Assert.That(options.AdditionalInboxes, Has.Count.EqualTo(1));
        Assert.That(options.AdditionalInboxes["priority"].WorkTransportUri, Is.EqualTo(new Uri("transport://./inbox-work-priority")));
        Assert.That(options.AdditionalInboxes["priority"].ThreadCount, Is.EqualTo(2));
        Assert.That(options.AdditionalInboxes["priority"].DeferredTransportUri, Is.Null);
    }

    [Test]
    public void Should_be_able_to_look_up_additional_inboxes_case_insensitively()
    {
        var options = GetOptions();

        Assert.That(options.AdditionalInboxes.Comparer, Is.EqualTo(StringComparer.OrdinalIgnoreCase));
        Assert.That(options.AdditionalInboxes["PRIORITY"].ThreadCount, Is.EqualTo(2));
    }
}
