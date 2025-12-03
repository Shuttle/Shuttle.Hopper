using NUnit.Framework;

namespace Shuttle.Hopper.Tests;

[TestFixture]
public class ProcessorThreadOptionsFixture : OptionsFixture
{
    [Test]
    public void Should_be_able_to_load_a_valid_configuration()
    {
        var options = GetOptions();

        Assert.That(options, Is.Not.Null);
        Assert.That(options.Threading.IsBackground, Is.False);
        Assert.That(options.Threading.JoinTimeout, Is.EqualTo(TimeSpan.FromSeconds(15)));
        Assert.That(options.Threading.Priority, Is.EqualTo(ThreadPriority.Lowest));
    }
}