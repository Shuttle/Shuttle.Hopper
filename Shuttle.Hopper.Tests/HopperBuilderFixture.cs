using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using NUnit.Framework;
using Shuttle.Threading;

namespace Shuttle.Hopper.Tests;

[TestFixture]
public class HopperBuilderFixture
{
    [Test]
    public void Should_throw_when_registering_the_same_inbox_twice()
    {
        var builder = new ServiceCollection().AddHopper();

        builder.AddInbox("priority");

        Assert.Throws<InvalidOperationException>(() => builder.AddInbox("priority"));
        Assert.Throws<InvalidOperationException>(() => builder.AddInbox("PRIORITY"));
    }

    [Test]
    public void Should_register_keyed_processor_and_idle_options_for_an_additional_inbox()
    {
        var serviceProvider = new ServiceCollection()
            .AddHopper()
            .AddInbox("Priority")
            .Services
            .BuildServiceProvider();

        var serviceKey = InboxProcessor.GetServiceKey("priority");

        Assert.That(serviceKey, Is.EqualTo("InboxProcessor:priority"));
        Assert.That(serviceProvider.GetRequiredService<IServiceProviderIsKeyedService>().IsKeyedService(typeof(IProcessor), serviceKey), Is.True);
        Assert.That(serviceProvider.GetRequiredService<IOptionsMonitor<ProcessorIdleOptions>>().Get(serviceKey).Durations, Is.Not.Empty);
    }

    [Test]
    public void Should_merge_code_configuration_into_configured_additional_inbox()
    {
        var serviceProvider = new ServiceCollection()
            .AddHopper(options =>
            {
                options.AdditionalInboxes = new()
                {
                    ["priority"] = new() { ThreadCount = 2 }
                };
            })
            .AddInbox("PRIORITY", options => options.WorkTransportUri = new("memory://memory/work-priority"))
            .Services
            .BuildServiceProvider();

        var options = serviceProvider.GetRequiredService<IOptions<HopperOptions>>().Value;

        Assert.That(options.AdditionalInboxes, Has.Count.EqualTo(1));
        Assert.That(options.AdditionalInboxes["Priority"].ThreadCount, Is.EqualTo(2));
        Assert.That(options.AdditionalInboxes["Priority"].WorkTransportUri, Is.EqualTo(new Uri("memory://memory/work-priority")));
    }
}
