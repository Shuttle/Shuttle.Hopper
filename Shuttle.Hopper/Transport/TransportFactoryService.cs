using Shuttle.Core.Contract;
using Shuttle.Core.Reflection;

namespace Shuttle.Hopper;

public class TransportFactoryService : ITransportFactoryService
{
    private readonly List<ITransportFactory> _transportFactories = [];
    private bool _disposed;

    public TransportFactoryService(IEnumerable<ITransportFactory>? transportFactories = null)
    {
        _transportFactories.AddRange(transportFactories ?? []);
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        foreach (var transportFactory in _transportFactories)
        {
            transportFactory.TryDispose();
        }

        _transportFactories.Clear();

        _disposed = true;
    }

    public ITransportFactory Get(string scheme)
    {
        Guard.AgainstEmpty(scheme);

        return Find(scheme) ?? throw new TransportFactoryNotFoundException(scheme);
    }

    public IEnumerable<ITransportFactory> Factories => _transportFactories.AsReadOnly();

    public ITransportFactory? Find(string scheme)
    {
        Guard.AgainstEmpty(scheme);

        return _transportFactories.FirstOrDefault(factory => factory.Scheme.Equals(scheme, StringComparison.InvariantCultureIgnoreCase));
    }

    public void Register(ITransportFactory transportFactory)
    {
        var factory = Find(Guard.AgainstNull(transportFactory).Scheme);

        if (factory != null)
        {
            _transportFactories.Remove(factory);
        }

        _transportFactories.Add(transportFactory);
    }

    public bool Contains(string scheme)
    {
        return Find(scheme) != null;
    }

    public ValueTask DisposeAsync()
    {
        Dispose();

        return new();
    }
}