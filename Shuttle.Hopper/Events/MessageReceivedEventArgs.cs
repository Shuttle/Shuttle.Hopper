using Shuttle.Core.Contract;

namespace Shuttle.Hopper;

public class MessageReceivedEventArgs(ITransport transport, ReceivedMessage receivedMessage)
{
    public ITransport Transport { get; } = Guard.AgainstNull(transport);
    public ReceivedMessage ReceivedMessage { get; } = Guard.AgainstNull(receivedMessage);
}