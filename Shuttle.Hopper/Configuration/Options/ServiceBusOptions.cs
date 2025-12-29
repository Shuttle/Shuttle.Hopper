using Shuttle.Extensions.Options;

namespace Shuttle.Hopper;

public class ServiceBusOptions
{
    public const string SectionName = "Shuttle:ServiceBus";

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

    public bool AddMessageHandlers { get; set; } = true;
    public bool CacheIdentity { get; set; } = true;
    public string CompressionAlgorithm { get; set; } = string.Empty;
    public bool CreatePhysicalTransports { get; set; } = true;
    public AsyncEvent<DeferredMessageProcessingAdjustedEventArgs> DeferredMessageProcessingAdjusted { get; set; } = new();
    public AsyncEvent<DeferredMessageProcessingHaltedEventArgs> DeferredMessageProcessingHalted { get; set; } = new();
    public string EncryptionAlgorithm { get; set; } = string.Empty;
    public AsyncEvent<HandlerExceptionEventArgs> HandlerException { get; set; } = new();
    public InboxOptions Inbox { get; set; } = new();
    public AsyncEvent<MessageAcknowledgedEventArgs> MessageAcknowledged { get; set; } = new();
    public AsyncEvent<DeserializationExceptionEventArgs> MessageDeserializationException { get; set; } = new();
    public AsyncEvent<MessageSentEventArgs> MessageSent { get; set; } = new();
    public AsyncEvent<MessageNotHandledEventArgs> MessageNotHandled { get; set; } = new();
    public AsyncEvent<MessageReceivedEventArgs> MessageReceived { get; set; } = new();
    public AsyncEvent<MessageReleasedEventArgs> MessageReleased { get; set; } = new();

    public AsyncEvent<MessageReturnedEventArgs> MessageReturned { get; set; } = new();
    public List<MessageRouteOptions> MessageRoutes { get; set; } = [];
    public AsyncEvent<TransportOperationEventArgs> TransportOperation { get; set; } = new();
    public OutboxOptions Outbox { get; set; } = new();
    public AsyncEvent<TransportEventArgs> TransportCreated { get; set; } = new();
    public AsyncEvent<TransportEventArgs> TransportDisposed { get; set; } = new();
    public AsyncEvent<TransportEventArgs> TransportDisposing { get; set; } = new();
    public bool RemoveCorruptMessages { get; set; } = false;
    public bool RemoveMessagesNotHandled { get; set; } = false;
    public SubscriptionOptions Subscription { get; set; } = new();
    public AsyncEvent<TransportMessageDeferredEventArgs> TransportMessageDeferred { get; set; } = new();
    public AsyncEvent<DeserializationExceptionEventArgs> TransportMessageDeserializationException { get; set; } = new();
    public List<UriMappingOptions> UriMappings { get; set; } = [];
}