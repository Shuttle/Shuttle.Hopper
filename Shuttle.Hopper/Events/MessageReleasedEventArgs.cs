using Shuttle.Core.Contract;

namespace Shuttle.Hopper;

public class MessageReleasedEventArgs(object acknowledgementToken) : EventArgs
{
    public object AcknowledgementToken { get; } = Guard.AgainstNull(acknowledgementToken);
}