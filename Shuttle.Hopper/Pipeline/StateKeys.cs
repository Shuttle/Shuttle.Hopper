namespace Shuttle.Hopper;

public static class StateKeys
{
    public const string DeferredMessageReturned = "DeferredMessageReturned";
    public const string DeferredTransport = "DeferredTransport";
    public const string DurationToIgnoreOnFailure = "DurationToIgnoreOnFailure";
    public const string ErrorTransport = "ErrorTransport";
    public const string HandlerContext = "HandlerContext";
    public const string MaximumFailureCount = "MaximumFailureCount";
    public const string Message = "Message";
    public const string MessageBytes = "MessageBytes";
    public const string MessageHandlerInvokeResult = "MessageHandlerInvokeResult";
    public const string ReceivedMessage = "ReceivedMessage";
    public const string TransportMessage = "TransportMessage";
    public const string TransportMessageBuilder = "TransportMessageBuilder";
    public const string TransportMessageReceived = "TransportMessageReceived";
    public const string TransportMessageStream = "TransportMessageStream";
    public const string WorkTransport = "WorkTransport";
}