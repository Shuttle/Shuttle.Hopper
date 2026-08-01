using Shuttle.Contract;
using Shuttle.Pipelines;

namespace Shuttle.Hopper;

public class MessageReceivedEventArgs(ITransport transport, ReceivedMessage receivedMessage, IPipeline pipeline) : PipelineEventArgs(pipeline)
{
    public ReceivedMessage ReceivedMessage { get; } = Guard.AgainstNull(receivedMessage);
    public ITransport Transport { get; } = Guard.AgainstNull(transport);
}