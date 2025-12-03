using Shuttle.Core.Contract;
using Shuttle.Core.Specification;

namespace Shuttle.Hopper;

public class StartsWithMessageRouteSpecification(string startWith) : ISpecification<string>
{
    private readonly string _startWith = Guard.AgainstEmpty(startWith).ToLower();

    public bool IsSatisfiedBy(string messageType)
    {
        return Guard.AgainstNull(messageType).ToLower().StartsWith(_startWith);
    }
}