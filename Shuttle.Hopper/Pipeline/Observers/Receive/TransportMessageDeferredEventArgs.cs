using Shuttle.Core.Contract;

namespace Shuttle.Hopper;

public class TransportMessageDeferredEventArgs(TransportMessage transportMessage) : EventArgs
{
    public TransportMessage TransportMessage { get; } = Guard.AgainstNull(transportMessage);
}