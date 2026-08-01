using Shuttle.Contract;
using Shuttle.Pipelines;

namespace Shuttle.Hopper;

public class MessageAcknowledgedEventArgs(ITransport transport, object acknowledgementToken, IPipeline pipeline) : PipelineEventArgs(pipeline)
{
    public object AcknowledgementToken { get; } = Guard.AgainstNull(acknowledgementToken);
    public ITransport Transport { get; } = Guard.AgainstNull(transport);
}