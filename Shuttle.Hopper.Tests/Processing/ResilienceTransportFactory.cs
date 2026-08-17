using System.Collections.Concurrent;
using Microsoft.Extensions.Options;
using Shuttle.Contract;

namespace Shuttle.Hopper.Tests;

public class ResilienceTransportFactory(IOptions<HopperOptions> hopperOptions) : ITransportFactory
{
    private readonly HopperOptions _hopperOptions = Guard.AgainstNull(Guard.AgainstNull(hopperOptions).Value);
    private readonly ConcurrentDictionary<Uri, ResilienceTransport> _transports = new();

    public string Scheme => "resilience";

    public Task<ITransport> CreateAsync(Uri uri, CancellationToken cancellationToken = default)
    {
        return Task.FromResult<ITransport>(_transports.GetOrAdd(Guard.AgainstNull(uri), _ => new(_hopperOptions, uri)));
    }

    public ResilienceTransport Get(string uri)
    {
        return _transports[new(uri)];
    }
}
