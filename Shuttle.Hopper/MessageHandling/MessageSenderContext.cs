namespace Shuttle.Hopper;

public class MessageSenderContext : IMessageSenderContext
{
    public TransportMessage? TransportMessage { get; set; }
}