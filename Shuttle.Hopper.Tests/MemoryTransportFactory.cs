using Microsoft.Extensions.Options;
using Shuttle.Core.Contract;

namespace Shuttle.Hopper.Tests;

public class MemoryTransportFactory(IOptions<HopperOptions> hopperOptions) : ITransportFactory
{
    public string Scheme => "memory";

    public Task<ITransport> CreateAsync(Uri uri, CancellationToken cancellationToken = default)
    {
        return Task.FromResult<ITransport>(new MemoryTransport(Guard.AgainstNull(Guard.AgainstNull(hopperOptions).Value), uri));
    }
}