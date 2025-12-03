namespace Shuttle.Hopper;

public interface ITransportFactoryService : IDisposable, IAsyncDisposable
{
    IEnumerable<ITransportFactory> Factories { get; }
    bool Contains(string scheme);
    ITransportFactory? Find(string scheme);
    ITransportFactory Get(string scheme);
    void Register(ITransportFactory transportFactory);
}