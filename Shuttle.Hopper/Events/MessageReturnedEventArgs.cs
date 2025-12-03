using Shuttle.Core.Contract;

namespace Shuttle.Hopper;

public class MessageReturnedEventArgs(TransportMessage transportMessage, ReceivedMessage receivedMessage)
    : EventArgs
{
    public ReceivedMessage ReceivedMessage { get; } = Guard.AgainstNull(receivedMessage);
    public TransportMessage TransportMessage { get; } = Guard.AgainstNull(transportMessage);
}