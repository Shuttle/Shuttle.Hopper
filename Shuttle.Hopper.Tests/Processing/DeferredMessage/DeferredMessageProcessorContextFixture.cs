using Microsoft.Extensions.Options;
using NUnit.Framework;
using Shuttle.Core.Pipelines;

namespace Shuttle.Hopper.Tests;

[TestFixture]
public class DeferredMessageProcessorContextFixture
{
    [Test]
    public async Task Should_be_able_to_handle_deferred_message_simulation_async()
    {
        var deferredMessageProcessorContext = new DeferredMessageProcessorContext(Options.Create(new HopperOptions { Inbox = new() { DeferredTransportUri = new("null://.") } }));

        // We'll assume that there are 3 messages in the deferred queue that are waiting for a second.
        var now = DateTimeOffset.UtcNow;
        var transportMessageA = new TransportMessage
        {
            IgnoreUntil = now.AddSeconds(1)
        };
        var transportMessageB = new TransportMessage
        {
            IgnoreUntil = now.AddSeconds(1)
        };
        var transportMessageC = new TransportMessage
        {
            IgnoreUntil = now.AddSeconds(1)
        };

        Assert.That(deferredMessageProcessorContext.ShouldCheckDeferredMessages, Is.True);
        Assert.That(deferredMessageProcessorContext.CheckpointMessageId, Is.EqualTo(Guid.Empty));
        Assert.That(deferredMessageProcessorContext.IgnoreUntil, Is.GreaterThan(now));

        var state = new State();

        state.SetReceivedMessage(new(Stream.Null, "ack-token"));
        state.SetTransportMessage(transportMessageA);

        var result = await deferredMessageProcessorContext.GetResultAsync(state);

        Assert.That(result, Is.True);
        Assert.That(deferredMessageProcessorContext.ShouldCheckDeferredMessages, Is.True);
        Assert.That(deferredMessageProcessorContext.CheckpointMessageId, Is.EqualTo(transportMessageA.MessageId));
        Assert.That(deferredMessageProcessorContext.IgnoreUntil, Is.GreaterThan(now));

        state.SetTransportMessage(transportMessageB);

        result = await deferredMessageProcessorContext.GetResultAsync(state);

        Assert.That(result, Is.True);
        Assert.That(deferredMessageProcessorContext.ShouldCheckDeferredMessages, Is.True);
        Assert.That(deferredMessageProcessorContext.CheckpointMessageId, Is.EqualTo(transportMessageA.MessageId));
        Assert.That(deferredMessageProcessorContext.IgnoreUntil, Is.GreaterThan(now));

        state.SetTransportMessage(transportMessageC);

        result = await deferredMessageProcessorContext.GetResultAsync(state);

        Assert.That(result, Is.True);
        Assert.That(deferredMessageProcessorContext.ShouldCheckDeferredMessages, Is.True);
        Assert.That(deferredMessageProcessorContext.CheckpointMessageId, Is.EqualTo(transportMessageA.MessageId));
        Assert.That(deferredMessageProcessorContext.IgnoreUntil, Is.GreaterThan(now));

        state.SetTransportMessage(transportMessageA);

        result = await deferredMessageProcessorContext.GetResultAsync(state);

        Assert.That(result, Is.True);
        Assert.That(deferredMessageProcessorContext.ShouldCheckDeferredMessages, Is.False);
        Assert.That(deferredMessageProcessorContext.CheckpointMessageId, Is.EqualTo(Guid.Empty));
        Assert.That(deferredMessageProcessorContext.IgnoreUntil, Is.GreaterThan(now));
    }
}