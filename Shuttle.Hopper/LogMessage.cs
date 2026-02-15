using Microsoft.Extensions.Logging;

namespace Shuttle.Hopper;

public static class LogMessage
{
    private static readonly Action<ILogger, string, string, string, Exception?> OperationDelegate =
        LoggerMessage.Define<string, string, string>(LogLevel.Trace, new(1000, nameof(Operation)), "Transport {TransportName} ({Scheme}) executing operation {Operation}");

    private static readonly Action<ILogger, string, string, Exception?> MessageAcknowledgedDelegate =
        LoggerMessage.Define<string, string>(LogLevel.Trace, new(1001, nameof(MessageAcknowledged)), "Message acknowledged on transport {TransportName} ({Scheme})");

    private static readonly Action<ILogger, Guid, string, string, string, Exception?> MessageEnqueuedDelegate =
        LoggerMessage.Define<Guid, string, string, string>(LogLevel.Trace, new(1002, nameof(MessageEnqueued)), "Message {MessageId} of type {MessageType} enqueued to transport {TransportName} ({Scheme})");
    
    private static readonly Action<ILogger, string, string, Exception?> MessageReceivedDelegate =
        LoggerMessage.Define<string, string>(LogLevel.Trace, new(1003, nameof(MessageReceived)), "Message received on transport {TransportName} ({Scheme})");

    private static readonly Action<ILogger, string, string, Exception?> MessageReleasedDelegate =
        LoggerMessage.Define<string, string>(LogLevel.Trace, new(1004, nameof(MessageReleased)), "Message released on transport {TransportName} ({Scheme})");

    public static void Operation(ILogger logger, string scheme, string transportName, string operation) =>
        OperationDelegate(logger, scheme, transportName, operation, null);

    public static void MessageAcknowledged(ILogger logger, string scheme, string transportName) =>
        MessageAcknowledgedDelegate(logger, scheme, transportName, null);

    public static void MessageEnqueued(ILogger logger, string scheme, string transportName, string messageType, Guid messageId) =>
        MessageEnqueuedDelegate(logger, messageId, messageType, transportName, scheme, null);
    
    public static void MessageReceived(ILogger logger, string scheme, string transportName) =>
        MessageReceivedDelegate(logger, scheme, transportName, null);

    public static void MessageReleased(ILogger logger, string scheme, string transportName) =>
        MessageReleasedDelegate(logger, scheme, transportName, null);
}
