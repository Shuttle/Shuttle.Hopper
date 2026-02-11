namespace Shuttle.Hopper;

public interface IMessageSenderContext
{
    TransportMessage? TransportMessage { get; set; }
}