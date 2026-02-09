using Microsoft.Extensions.Options;
using Shuttle.Core.Contract;

namespace Shuttle.Hopper.Tests;

public class NullTransportFactory(IOptions<HopperOptions> hopperOptions) : ITransportFactory
{
    public string Scheme => "null-transport";

    public Task<ITransport> CreateAsync(Uri uri, CancellationToken cancellationToken = default)
    {
        return Task.FromResult<ITransport>(new NullTransport(Guard.AgainstNull(Guard.AgainstNull(hopperOptions).Value), Guard.AgainstNull(uri)));
    }
}