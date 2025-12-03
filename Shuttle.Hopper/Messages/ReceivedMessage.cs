using Shuttle.Core.Contract;

namespace Shuttle.Hopper;

public class ReceivedMessage(Stream stream, object acknowledgementToken)
{
    public object AcknowledgementToken { get; } = acknowledgementToken;

    public Stream Stream { get; } = Guard.AgainstNull(stream);
}