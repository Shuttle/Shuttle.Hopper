using Shuttle.Core.Contract;

namespace Shuttle.Hopper;

public class MessageAcknowledgedEventArgs(ITransport transport, object acknowledgementToken)
{
    public ITransport Transport { get; } = Guard.AgainstNull(transport);
    public object AcknowledgementToken { get; } = Guard.AgainstNull(acknowledgementToken);
}