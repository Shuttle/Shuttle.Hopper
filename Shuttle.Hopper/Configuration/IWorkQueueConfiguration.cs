namespace Shuttle.Hopper;

public interface IWorkTransportConfiguration
{
    ITransport? WorkTransport { get; set; }
}