namespace Shuttle.Hopper;

public interface IMessageContext
{
    ExceptionHandling ExceptionHandling { get; set; }
    TransportMessage TransportMessage { get; set; }
}