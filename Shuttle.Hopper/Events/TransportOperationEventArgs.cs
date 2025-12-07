using Shuttle.Core.Contract;

namespace Shuttle.Hopper;

public class TransportOperationEventArgs(ITransport transport, string operation, object? data = null)
{
    public object? Data { get; } = data;
    public ITransport Transport { get; } = Guard.AgainstNull(transport);
    public string Operation { get; } = Guard.AgainstEmpty(operation);
}