using System.Security.Principal;
using Microsoft.Extensions.Options;
using Shuttle.Core.Contract;

namespace Shuttle.Hopper;

public class DefaultIdentityProvider : IIdentityProvider
{
    private static IIdentity? _identity;
    private readonly bool _cache;

    public DefaultIdentityProvider(IOptions<HopperOptions> hopperOptions)
    {
        _cache = Guard.AgainstNull(Guard.AgainstNull(hopperOptions).Value).CacheIdentity;

        if (_cache)
        {
            _identity = new GenericIdentity(Environment.UserDomainName + "\\" + Environment.UserName, "Anonymous");
        }
    }

    public IIdentity Get()
    {
        return _cache ? _identity! : new GenericIdentity(Environment.UserDomainName + "\\" + Environment.UserName, "Anonymous");
    }
}