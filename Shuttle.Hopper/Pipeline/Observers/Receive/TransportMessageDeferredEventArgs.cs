using Shuttle.Contract;

namespace Shuttle.Hopper;

public class TransportMessageDeferredEventArgs(TransportMessage transportMessage)
{
    public TransportMessage TransportMessage { get; } = Guard.AgainstNull(transportMessage);
}