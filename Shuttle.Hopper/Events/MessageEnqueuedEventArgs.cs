using Shuttle.Core.Contract;

namespace Shuttle.Hopper;

public class MessageEntransportdEventArgs(TransportMessage message, Stream stream) : EventArgs
{
    public Stream Stream { get; } = Guard.AgainstNull(stream);
    public TransportMessage TransportMessage { get; } = Guard.AgainstNull(message);
}