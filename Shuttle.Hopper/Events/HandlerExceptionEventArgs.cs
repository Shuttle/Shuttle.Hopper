using Shuttle.Pipelines;

namespace Shuttle.Hopper;

public class HandlerExceptionEventArgs(TransportMessage transportMessage, object message, Exception exception, IPipeline pipeline)
    : PipelineEventArgs(pipeline)
{
    public Exception Exception { get; } = exception;
    public object Message { get; } = message;
    public TransportMessage TransportMessage { get; } = transportMessage;
}