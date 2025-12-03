using Shuttle.Core.Contract;
using Shuttle.Core.Pipelines;

namespace Shuttle.Hopper;

public class MessageNotHandledEventArgs(IPipelineContext pipelineContext, TransportMessage transportMessage, object message)
    : PipelineContextEventArgs(pipelineContext)
{
    public object Message { get; } = Guard.AgainstNull(message);

    public TransportMessage TransportMessage { get; } = Guard.AgainstNull(transportMessage);
}