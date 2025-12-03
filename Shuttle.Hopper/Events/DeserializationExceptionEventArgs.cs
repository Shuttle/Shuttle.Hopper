using Shuttle.Core.Pipelines;

namespace Shuttle.Hopper;

public class DeserializationExceptionEventArgs(IPipelineContext pipelineContext, ITransport workTransport, ITransport errorTransport, Exception exception)
    : PipelineContextEventArgs(pipelineContext)
{
    public ITransport ErrorTransport { get; } = errorTransport;
    public Exception Exception { get; } = exception;

    public ITransport WorkTransport { get; } = workTransport;
}