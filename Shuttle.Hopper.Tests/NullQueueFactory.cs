using Microsoft.Extensions.Options;
using Shuttle.Core.Contract;

namespace Shuttle.Hopper.Tests;

public class NullTransportFactory(IOptions<ServiceBusOptions> serviceBusOptions) : ITransportFactory
{
    public string Scheme => "null-transport";

    public ITransport Create(Uri uri)
    {
        return new NullTransport(Guard.AgainstNull(Guard.AgainstNull(serviceBusOptions).Value), Guard.AgainstNull(uri));
    }
}