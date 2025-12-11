using Shuttle.Core.Contract;

namespace Shuttle.Hopper;

public class MessageSentEventArgs(ITransport transport, TransportMessage message, Stream stream)
{
    public ITransport Transport { get; } = Guard.AgainstNull(transport);
    public Stream Stream { get; } = Guard.AgainstNull(stream);
    public TransportMessage TransportMessage { get; } = Guard.AgainstNull(message);
}