namespace Shuttle.Hopper;

public interface IOutboxConfiguration :
    IWorkTransportConfiguration,
    IErrorTransportConfiguration
{
}