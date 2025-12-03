namespace Shuttle.Hopper;

public interface IErrorTransportConfiguration
{
    ITransport? ErrorTransport { get; set; }
}