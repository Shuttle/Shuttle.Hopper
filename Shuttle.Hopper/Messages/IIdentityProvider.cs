using System.Security.Principal;

namespace Shuttle.Hopper;

public interface IIdentityProvider
{
    IIdentity Get();
}