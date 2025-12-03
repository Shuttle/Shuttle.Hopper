using Microsoft.Extensions.Options;
using NUnit.Framework;

namespace Shuttle.Hopper.Tests;

[TestFixture]
public class MessageRouteProviderFixture
{
    [Test]
    public async Task Should_be_able_to_add_or_merge_message_routes()
    {
        var nullTransportUri = new Uri("null://transport/");
        const string firstMessageType = "first-message-type";
        const string secondMessageType = "second-message-type";

        var provider = new MessageRouteProvider(Options.Create(new ServiceBusOptions()));

        Assert.That((await provider.GetRouteUrisAsync(firstMessageType)).Any(), Is.False);

        await provider.AddAsync(new MessageRoute(nullTransportUri).AddSpecification(new StartsWithMessageRouteSpecification("first")));

        Assert.That((await provider.GetRouteUrisAsync(firstMessageType)).Any(), Is.True);
        Assert.That((await provider.GetRouteUrisAsync(secondMessageType)).Any(), Is.False);
        Assert.That((await provider.GetRouteUrisAsync(firstMessageType)).First(), Is.EqualTo(nullTransportUri));

        await provider.AddAsync(new MessageRoute(nullTransportUri).AddSpecification(new StartsWithMessageRouteSpecification("second")));

        Assert.That((await provider.GetRouteUrisAsync(firstMessageType)).Any(), Is.True);
        Assert.That((await provider.GetRouteUrisAsync(secondMessageType)).Any(), Is.True);
        Assert.That((await provider.GetRouteUrisAsync(firstMessageType)).First(), Is.EqualTo(nullTransportUri));
        Assert.That((await provider.GetRouteUrisAsync(secondMessageType)).First(), Is.EqualTo(nullTransportUri));
    }
}