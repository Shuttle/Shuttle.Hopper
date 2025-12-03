using Shuttle.Core.Contract;

namespace Shuttle.Hopper;

public class TransportEventArgs(ITransport transport) : EventArgs
{
    public ITransport Transport { get; } = Guard.AgainstNull(transport);
}