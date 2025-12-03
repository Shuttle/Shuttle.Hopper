namespace Shuttle.Hopper;

public interface ITransportFactory
{
    string Scheme { get; }
    ITransport Create(Uri uri);
}