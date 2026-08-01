using Shuttle.Contract;
using Shuttle.Pipelines;

namespace Shuttle.Hopper;

public class MessageReleasedEventArgs(ITransport transport, object acknowledgementToken, IPipeline pipeline) : PipelineEventArgs(pipeline)
{
    public ITransport Transport { get; } = Guard.AgainstNull(transport);
    public object AcknowledgementToken { get; } = Guard.AgainstNull(acknowledgementToken);
}