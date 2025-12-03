using Shuttle.Core.Pipelines;

namespace Shuttle.Hopper;

public class HandlerExceptionEventArgs(IPipelineContext pipelineContext, TransportMessage transportMessage, object message, Exception exception)
    : PipelineContextEventArgs(pipelineContext)
{
    public Exception Exception { get; } = exception;
    public object Message { get; } = message;
    public TransportMessage TransportMessage { get; } = transportMessage;
}