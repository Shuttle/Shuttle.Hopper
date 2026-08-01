using Shuttle.Contract;
using Shuttle.Pipelines;

namespace Shuttle.Hopper;

public class MessageSentEventArgs(ITransport transport, Stream stream, IPipeline pipeline) : PipelineEventArgs(pipeline)
{
    public Stream Stream { get; } = Guard.AgainstNull(stream);
    public ITransport Transport { get; } = Guard.AgainstNull(transport);
}