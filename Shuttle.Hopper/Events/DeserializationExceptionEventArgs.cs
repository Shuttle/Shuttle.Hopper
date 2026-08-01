using Shuttle.Pipelines;

namespace Shuttle.Hopper;

public class DeserializationExceptionEventArgs(ITransport workTransport, ITransport errorTransport, Exception exception, IPipeline pipeline)
    : PipelineEventArgs(pipeline)
{
    public ITransport ErrorTransport { get; } = errorTransport;
    public Exception Exception { get; } = exception;
    public ITransport WorkTransport { get; } = workTransport;
}