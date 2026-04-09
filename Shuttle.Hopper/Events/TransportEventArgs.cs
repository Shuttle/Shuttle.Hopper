using Shuttle.Contract;

namespace Shuttle.Hopper;

public class TransportEventArgs(ITransport transport)
{
    public ITransport Transport { get; } = Guard.AgainstNull(transport);
}