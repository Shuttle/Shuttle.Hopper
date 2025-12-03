using Shuttle.Core.Contract;

namespace Shuttle.Hopper;

public class MessageReceivedEventArgs(ReceivedMessage receivedMessage) : EventArgs
{
    public ReceivedMessage ReceivedMessage { get; } = Guard.AgainstNull(receivedMessage);
}