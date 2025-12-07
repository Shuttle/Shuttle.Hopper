using Shuttle.Core.Contract;

namespace Shuttle.Hopper;

public class TransportOperationEventArgs(ITransport transport, string operation)
{
    public ITransport Transport { get; } = Guard.AgainstNull(transport);
    public string Operation { get; } = Guard.AgainstEmpty(operation);
}