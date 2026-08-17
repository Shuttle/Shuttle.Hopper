using Shuttle.Extensions.Options;
using Shuttle.Pipelines;

namespace Shuttle.Hopper;

public class HopperOptions
{
    public const string SectionName = "Shuttle:Hopper";

    public static readonly IEnumerable<TimeSpan> DefaultIgnoreOnFailureDurations = new List<TimeSpan>
    {
        TimeSpan.FromSeconds(30),
        TimeSpan.FromMinutes(2),
        TimeSpan.FromMinutes(5)
    }.AsReadOnly();

    public static readonly IEnumerable<TimeSpan> DefaultIdleDurations = new List<TimeSpan>
    {
        TimeSpan.FromMilliseconds(250),
        TimeSpan.FromMilliseconds(250),
        TimeSpan.FromMilliseconds(250),
        TimeSpan.FromMilliseconds(250),
        TimeSpan.FromMilliseconds(500),
        TimeSpan.FromMilliseconds(500),
        TimeSpan.FromSeconds(1),
        TimeSpan.FromSeconds(5)
    }.AsReadOnly();

    public bool CacheIdentity { get; set; } = true;
    public bool CreatePhysicalTransports { get; set; } = true;
    public AsyncEvent<DeferredMessageProcessingAdjustedEventArgs> DeferredMessageProcessingAdjusted { get; set; } = new();
    public AsyncEvent<DeferredMessageProcessingHaltedEventArgs> DeferredMessageProcessingHalted { get; set; } = new();
    public AsyncEvent<HandlerExceptionEventArgs> HandlerException { get; set; } = new();
    public InboxOptions Inbox { get; set; } = new();
    /// <summary>
    /// Called when the message processing completed successfully.
    /// </summary>
    public AsyncEvent<MessageAcknowledgedEventArgs> MessageAcknowledged { get; set; } = new();
    /// <summary>
    /// Called when the received message stream cannot be deserialized successfully.
    /// </summary>
    public AsyncEvent<DeserializationExceptionEventArgs> MessageDeserializationException { get; set; } = new();
    /// <summary>
    /// Called when there is no handler registered that accepts the message type.
    /// </summary>
    public AsyncEvent<MessageNotHandledEventArgs> MessageNotHandled { get; set; } = new();
    /// <summary>
    /// Called when a message is received from the transport.
    /// </summary>
    public AsyncEvent<MessageReceivedEventArgs> MessageReceived { get; set; } = new();
    /// <summary>
    /// Called when a message is released back to the transport.
    /// </summary>
    public AsyncEvent<MessageReleasedEventArgs> MessageReleased { get; set; } = new();
    /// <summary>
    /// Called when a deferred message has been returned to the inbox work queue.
    /// </summary>
    public AsyncEvent<PipelineEventArgs> DeferredMessageReturned { get; set; } = new();
    public List<MessageRouteOptions> MessageRoutes { get; set; } = [];
    /// <summary>
    /// Called when a message has been sent to the transport.
    /// </summary>
    public AsyncEvent<MessageSentEventArgs> MessageSent { get; set; } = new();
    public OutboxOptions Outbox { get; set; } = new();
    public bool RemoveCorruptMessages { get; set; } = false;
    public bool RemoveMessagesNotHandled { get; set; } = false;
    public SubscriptionOptions Subscription { get; set; } = new();
    public bool AutoStart { get; set; } = true;
    public AsyncEvent<TransportEventArgs> TransportCreated { get; set; } = new();
    public AsyncEvent<TransportEventArgs> TransportDisposed { get; set; } = new();
    public AsyncEvent<TransportEventArgs> TransportDisposing { get; set; } = new();
    public AsyncEvent<TransportMessageDeferredEventArgs> TransportMessageDeferred { get; set; } = new();
    public AsyncEvent<DeserializationExceptionEventArgs> TransportMessageDeserializationException { get; set; } = new();
    public AsyncEvent<TransportOperationEventArgs> TransportOperation { get; set; } = new();
    public List<UriMappingOptions> UriMappings { get; set; } = [];
}