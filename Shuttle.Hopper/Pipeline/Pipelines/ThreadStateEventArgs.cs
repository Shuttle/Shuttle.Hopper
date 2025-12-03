using Shuttle.Core.Contract;
using Shuttle.Core.Pipelines;

namespace Shuttle.Hopper;

public class ThreadStateEventArgs(IPipeline pipeline) : EventArgs
{
    public IPipeline Pipeline { get; } = Guard.AgainstNull(pipeline);
}